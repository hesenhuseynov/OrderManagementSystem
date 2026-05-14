namespace OrderManagementSystem.Features.Orders.Pay
{
    public sealed record PayOrderResponse(
        int OrderId,
        string OrderStatus,
        int PaymentId,
        string PaymentStatus,
        string? ExternalTransactionId,
        string Message);

}
