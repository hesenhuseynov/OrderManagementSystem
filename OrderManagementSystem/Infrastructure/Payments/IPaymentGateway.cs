namespace OrderManagementSystem.Infrastructure.Payments
{
    public interface IPaymentGateway
    {
        Task<PaymentResult> ChargeAsync(
            PaymentCommand command,
            CancellationToken cancellationToken);
    }

    public sealed record PaymentCommand(
        Guid IdempotencyKey,
        int OrderId,
        decimal Amount,
        string Currency,
        string PaymentMethod);

    public sealed record PaymentResult(
        bool IsSuccess,
        string? TransactionId,
        string? FailureReason);
}
