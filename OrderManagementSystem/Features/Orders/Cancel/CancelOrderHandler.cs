using Dapper;
using FluentValidation;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Common.Caching;
using OrderManagementSystem.Common.Results;
using OrderManagementSystem.Common.Validation;
using OrderManagementSystem.Features.Orders.Create;
using OrderManagementSystem.Infrastructure;
using System.Data;
using System.Data.Common;

namespace OrderManagementSystem.Features.Orders.Cancel
{
    public sealed class CancelOrderHandler
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IValidator<CancelOrderRequest> _validator;
        private readonly IResilientCacheService _resilientCacheService;
        private readonly ILogger<CancelOrderHandler> _logger;

        public CancelOrderHandler(
            IDbConnectionFactory dbConnectionFactory,
            IValidator<CancelOrderRequest> validator,
            IResilientCacheService resilientCacheService,
            ILogger<CancelOrderHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(dbConnectionFactory);
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(resilientCacheService);
            ArgumentNullException.ThrowIfNull(logger);

            _dbConnectionFactory = dbConnectionFactory;
            _validator = validator;
            _resilientCacheService = resilientCacheService;
            _logger = logger;
        }

        public async Task<Result<CancelOrderResponse>> HandleAsync(
            CancelOrderRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return Result.Failure<CancelOrderResponse>(
                    validationResult.ToErrorList());
            }

            CancelOrderResponse? response = null;

            using (DbConnection connection = await  _dbConnectionFactory.CreateConnectionAsync(cancellationToken))
            {
                await  connection.OpenAsync(cancellationToken);

                using var transaction =  await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted,cancellationToken);

                try
                {
                    const string cancelOrderSql = """
                        UPDATE dbo.Orders
                        SET Status = @CancelledStatus
                        WHERE OrderId = @OrderId
                          AND Status = @PendingStatus;
                        """;

                    var updatedOrderRows = await connection.ExecuteAsync(
                        new CommandDefinition(
                            cancelOrderSql,
                            new
                            {
                                OrderId = request.Id,
                                CancelledStatus = OrderConstants.Statuses.Cancelled,
                                PendingStatus = OrderConstants.Statuses.Pending
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                    
                    if (updatedOrderRows == 0)
                    {
                        const string currentStatusSql = """
                            SELECT Status
                            FROM dbo.Orders
                            WHERE OrderId = @OrderId;
                            """;

                        var currentStatus = await connection.QueryFirstOrDefaultAsync<string>(
                            new CommandDefinition(
                                currentStatusSql,
                                new { OrderId = request.Id },
                                transaction: transaction,
                                cancellationToken: cancellationToken));

                        await transaction.RollbackAsync();

                        if (currentStatus is null)
                        {
                            return Result.Failure<CancelOrderResponse>(
                                OrderErrors.OrderNotFound(request.Id));
                        }

                        return Result.Failure<CancelOrderResponse>(
                            OrderErrors.InvalidStatusForCancellation(currentStatus));
                    }

                    const string orderItemsSql = """
                        SELECT
                            ProductId,
                            Quantity
                        FROM dbo.OrderItems
                        WHERE OrderId = @OrderId
                        ORDER BY ProductId;
                        """;

                    var orderItems = (await connection.QueryAsync<OrderItemSnapshot>(
                        new CommandDefinition(
                            orderItemsSql,
                            new { OrderId = request.Id },
                            transaction: transaction,
                            cancellationToken: cancellationToken)))
                        .ToList();

                    if (orderItems.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Order '{request.Id}' was cancelled but no order items were found.");
                    }

                    var stockParameters = orderItems
                        .GroupBy(x => x.ProductId)
                        .Select(g => new
                        {
                            ProductId = g.Key,
                            Quantity = g.Sum(x => x.Quantity)
                        })
                        .OrderBy(x => x.ProductId)
                        .ToList();

                    const string restoreStockSql = """
                        UPDATE dbo.Products
                        SET StockQuantity = StockQuantity + @Quantity
                        WHERE ProductId = @ProductId;
                        """;

                    var restoredRows = await connection.ExecuteAsync(
                        new CommandDefinition(
                            restoreStockSql,
                            stockParameters,
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                    if (restoredRows != stockParameters.Count)
                    {
                        throw new InvalidOperationException(
                            "Stock restore affected an unexpected number of rows.");
                    }

                    const string insertInventoryMovementsSql = """
                        INSERT INTO dbo.InventoryMovements
                        (
                            ProductId,
                            OrderId,
                            MovementType,
                            Quantity,
                            Reason
                        )
                        VALUES
                        (
                            @ProductId,
                            @OrderId,
                            @MovementType,
                            @Quantity,
                            @Reason
                        );
                        """;

                    var movementParameters = stockParameters.Select(item => new
                    {
                        item.ProductId,
                        OrderId = request.Id,
                        MovementType = OrderConstants.InventoryMovementTypes.Restock,
                        item.Quantity,
                        Reason = OrderConstants.Reasons.OrderCancelled
                    });

                    var movementRows = await connection.ExecuteAsync(
                        new CommandDefinition(
                            insertInventoryMovementsSql,
                            movementParameters,
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                    if (movementRows != stockParameters.Count)
                    {
                        throw new InvalidOperationException(
                            "Inventory movement insert affected an unexpected number of rows.");
                    }

                    const string insertOrderStatusHistorySql = """
                        INSERT INTO dbo.OrderStatusHistory
                        (
                            OrderId,
                            OldStatus,
                            NewStatus,
                            ChangedBy,
                            Reason
                        )
                        VALUES
                        (
                            @OrderId,
                            @OldStatus,
                            @NewStatus,
                            @ChangedBy,
                            @Reason
                        );
                        """;

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            insertOrderStatusHistorySql,
                            new
                            {
                                OrderId = request.Id,
                                OldStatus = OrderConstants.Statuses.Pending,
                                NewStatus = OrderConstants.Statuses.Cancelled,
                                ChangedBy = OrderConstants.ChangedBy.System,
                                Reason = OrderConstants.Reasons.OrderCancelled
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                    response = new CancelOrderResponse(
                        request.Id,
                        OrderConstants.Statuses.Cancelled);

                   await  transaction.CommitAsync();
                }
                catch
                {
                   await   transaction.RollbackAsync();
                    throw;
                }
            }

            if (response is null)
            {
                throw new InvalidOperationException(
                    "Order cancellation completed without building a response.");
            }

            
                await _resilientCacheService.TryRemoveAsync(
                    CacheKeys.OrderById(request.Id),
                    cancellationToken);


            return Result.Success(response);
        }

        private sealed class OrderItemSnapshot
        {
            public int ProductId { get; init; }
            public int Quantity { get; init; }
        }
    }
}
