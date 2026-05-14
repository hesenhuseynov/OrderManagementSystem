namespace OrderManagementSystem.Features.Orders.Pay.Contracts
{
    public sealed record PaymentCompletedEvent(int OrderId, int PaymentId, string TransactionId, decimal Amount, string Currency,
        DateTime PaidAt);

}
