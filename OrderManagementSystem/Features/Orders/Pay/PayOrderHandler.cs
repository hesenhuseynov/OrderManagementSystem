using Dapper;
using FluentValidation;
using Microsoft.Data.SqlClient;
using OrderManagementSystem.Common.Errors;
using OrderManagementSystem.Common.Outbox;
using OrderManagementSystem.Common.Results;
using OrderManagementSystem.Common.Validation;
using OrderManagementSystem.Features.Orders.Pay.Contracts;
using OrderManagementSystem.Infrastructure;
using OrderManagementSystem.Infrastructure.Payments;
using System.Data;

namespace OrderManagementSystem.Features.Orders.Pay
{
    public sealed class PayOrderHandler
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IOutboxWriter _outboxWriter;
        private readonly IValidator<PayOrderRequest> _validator;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<PayOrderHandler> _logger;

        public PayOrderHandler(
            IDbConnectionFactory dbConnectionFactory,
            IOutboxWriter outboxWriter,
            IValidator<PayOrderRequest> validator,
            IPaymentGateway paymentGateway,
            ILogger<PayOrderHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(dbConnectionFactory);
            ArgumentNullException.ThrowIfNull(outboxWriter);
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(paymentGateway);
            ArgumentNullException.ThrowIfNull(logger);

            _dbConnectionFactory = dbConnectionFactory;
            _outboxWriter = outboxWriter;
            _validator = validator;
            _paymentGateway = paymentGateway;
            _logger = logger;
        }

        public async Task<Result<PayOrderResponse>> HandleAsync(
            int orderId,
            PayOrderRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return Result.Failure<PayOrderResponse>(
                    validationResult.ToErrorList());
            }

            var idempotencyKey = request.IdempotencyKey!.Value;

            using var connection = await  _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
                 await   connection.OpenAsync(cancellationToken);

            using var transaction =  await connection.BeginTransactionAsync(IsolationLevel.Serializable,cancellationToken);

            try
            {
                var existingPayment = await GetExistingPaymentAsync(
                    connection,
                    transaction,
                    idempotencyKey,
                    cancellationToken);

                if (existingPayment is not null)
                {
                   await  transaction.RollbackAsync();

                    if (existingPayment.OrderId != orderId)
                    {
                        return Result.Failure<PayOrderResponse>(
                            PaymentErrors.IdempotencyKeyAlreadyUsedForAnotherOrder());
                    }

                    if (existingPayment.Status == PaymentConstants.Statuses.Succeeded)
                    {
                        return Result.Success(new PayOrderResponse(
                            OrderId: existingPayment.OrderId,
                            OrderStatus: OrderConstants.Statuses.Paid,
                            PaymentId: existingPayment.PaymentId,
                            PaymentStatus: existingPayment.Status,
                            ExternalTransactionId: existingPayment.ExternalTransactionId,
                            Message: "Payment was already processed with the same idempotency key."));
                    }

                    return Result.Failure<PayOrderResponse>(
                        PaymentErrors.PaymentAlreadyFailed());
                }

                var order = await GetOrderForPaymentAsync(
                    connection,
                    transaction,
                    orderId,
                    cancellationToken);

                if (order is null)
                {
                  await  transaction.RollbackAsync();

                    return Result.Failure<PayOrderResponse>(
                        PaymentErrors.OrderNotFound(orderId));
                }

                if (order.Status != OrderConstants.Statuses.Pending)
                {
                   await transaction.RollbackAsync();

                    return Result.Failure<PayOrderResponse>(
                        PaymentErrors.OrderCannotBePaid(orderId, order.Status));
                }

                var paymentCommand = new PaymentCommand(
                    IdempotencyKey: idempotencyKey,
                    OrderId: order.OrderId,
                    Amount: order.TotalAmount,
                    Currency: order.Currency,
                    PaymentMethod: request.PaymentMethod);

                var paymentResult = await _paymentGateway.ChargeAsync(
                    paymentCommand,
                    cancellationToken);

                if (!paymentResult.IsSuccess)
                {
                    var failedPaymentId = await InsertPaymentAsync(
                        connection,
                        transaction,
                        order,
                        idempotencyKey,
                        status: PaymentConstants.Statuses.Failed,
                        externalTransactionId: null,
                        failureReason: paymentResult.FailureReason ?? "Payment failed.",
                        cancellationToken);

                   await transaction.CommitAsync();

                    _logger.LogWarning(
                        "Payment declined. OrderId: {OrderId}, PaymentId: {PaymentId}, Reason: {Reason}",
                        order.OrderId,
                        failedPaymentId,
                        paymentResult.FailureReason);

                    return Result.Failure<PayOrderResponse>(
                        PaymentErrors.PaymentDeclined(paymentResult.FailureReason ?? "Payment failed."));
                }

                var paymentId = await InsertPaymentAsync(
                    connection,
                    transaction,
                    order,
                    idempotencyKey,
                    status: PaymentConstants.Statuses.Succeeded,
                    externalTransactionId: paymentResult.TransactionId,
                    failureReason: null,
                    cancellationToken);

                var rowsAffected = await MarkOrderAsPaidAsync(
                    connection,
                    transaction,
                    order.OrderId,
                    cancellationToken);

                if (rowsAffected == 0)
                {
                    await transaction.RollbackAsync();

                    return Result.Failure<PayOrderResponse>(
                        PaymentErrors.OrderCannotBePaid(orderId, order.Status));
                }

                await InsertOrderStatusHistoryAsync(
                    connection,
                    transaction,
                    order.OrderId,
                    oldStatus: OrderConstants.Statuses.Pending,
                    newStatus: OrderConstants.Statuses.Paid,
                    cancellationToken);

                var paidAt = DateTime.UtcNow;

                var paymentCompletedEvent = new PaymentCompletedEvent(
                    OrderId: order.OrderId,
                    PaymentId: paymentId,
                    TransactionId: paymentResult.TransactionId!,
                    Amount: order.TotalAmount,
                    Currency: order.Currency,
                    PaidAt: paidAt);

                await _outboxWriter.AddAsync(
                    connection,
                    transaction,
                    OutBoxEventTypes.PaymentCompleted,
                    aggregateType: "Order",
                    aggregateId: order.OrderId.ToString(),
                    payload: paymentCompletedEvent,
                    cancellationToken);

               await  transaction.CommitAsync();

                return Result.Success(new PayOrderResponse(
                    OrderId: order.OrderId,
                    OrderStatus: OrderConstants.Statuses.Paid,
                    PaymentId: paymentId,
                    PaymentStatus: PaymentConstants.Statuses.Succeeded,
                    ExternalTransactionId: paymentResult.TransactionId,
                    Message: "Payment completed successfully."));
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
              await  transaction.RollbackAsync();

                return Result.Failure<PayOrderResponse>(
                    Error.Conflict(
                        "payment.idempotency_conflict",
                        "A payment with the same idempotency key already exists."));
            }
            catch
            {
              await   transaction.RollbackAsync();
                throw;
            }
        }

        private static async Task<OrderSnapshot?> GetOrderForPaymentAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            int orderId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT
                    OrderId,
                    Status,
                    TotalAmount,
                    Currency
                FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK)
                WHERE OrderId = @OrderId;
                """;

            return await connection.QuerySingleOrDefaultAsync<OrderSnapshot>(
                new CommandDefinition(
                    sql,
                    new { OrderId = orderId },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        private static async Task<PaymentSnapshot?> GetExistingPaymentAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            Guid idempotencyKey,
            CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT TOP (1)
                    PaymentId,
                    OrderId,
                    Status,
                    ExternalTransactionId
                FROM dbo.Payments
                WHERE IdempotencyKey = @IdempotencyKey;
                """;

            return await connection.QuerySingleOrDefaultAsync<PaymentSnapshot>(
                new CommandDefinition(
                    sql,
                    new { IdempotencyKey = idempotencyKey },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        private static async Task<int> InsertPaymentAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            OrderSnapshot order,
            Guid idempotencyKey,
            string status,
            string? externalTransactionId,
            string? failureReason,
            CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO dbo.Payments
                (
                    OrderId,
                    Provider,
                    Status,
                    Amount,
                    Currency,
                    IdempotencyKey,
                    ExternalTransactionId,
                    FailureReason,
                    CreatedAt,
                    ProcessedAt
                )
                OUTPUT INSERTED.PaymentId
                VALUES
                (
                    @OrderId,
                    @Provider,
                    @Status,
                    @Amount,
                    @Currency,
                    @IdempotencyKey,
                    @ExternalTransactionId,
                    @FailureReason,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                );
                """;

            return await connection.QuerySingleAsync<int>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        order.OrderId,
                        Provider = PaymentConstants.Providers.Fake,
                        Status = status,
                        Amount = order.TotalAmount,
                        Currency = order.Currency,
                        IdempotencyKey = idempotencyKey,
                        ExternalTransactionId = externalTransactionId,
                        FailureReason = failureReason
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        private static async Task<int> MarkOrderAsPaidAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            int orderId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE dbo.Orders
                SET
                    Status = @PaidStatus,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE OrderId = @OrderId
                  AND Status = @PendingStatus;
                """;

            return await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        OrderId = orderId,
                        PaidStatus = OrderConstants.Statuses.Paid,
                        PendingStatus = OrderConstants.Statuses.Pending
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        private static async Task InsertOrderStatusHistoryAsync(
            IDbConnection connection,
            IDbTransaction transaction,
            int orderId,
            string oldStatus,
            string newStatus,
            CancellationToken cancellationToken)
        {
            const string sql = """
                INSERT INTO dbo.OrderStatusHistory
                (
                    OrderId,
                    OldStatus,
                    NewStatus,
                    ChangedAt,
                    ChangedBy,
                    Reason
                )
                VALUES
                (
                    @OrderId,
                    @OldStatus,
                    @NewStatus,
                    SYSUTCDATETIME(),
                    @ChangedBy,
                    @Reason
                );
                """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        OrderId = orderId,
                        OldStatus = oldStatus,
                        NewStatus = newStatus,
                        ChangedBy = OrderConstants.ChangedBy.System,
                        Reason = OrderConstants.Reasons.OrderPaid
                    },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        private sealed record OrderSnapshot(
            int OrderId,
            string Status,
            decimal TotalAmount,
            string Currency);

        private sealed record PaymentSnapshot(
            int PaymentId,
            int OrderId,
            string Status,
            string? ExternalTransactionId);
    }
}