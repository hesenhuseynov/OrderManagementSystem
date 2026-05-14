namespace OrderManagementSystem.Features.Orders.Pay
{

    public sealed record PayOrderRequest(
           string PaymentMethod,
           string? CardLast4,
           Guid? IdempotencyKey);
} 