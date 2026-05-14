using OrderManagementSystem.Features.Orders.Pay;

namespace OrderManagementSystem.Infrastructure.Payments
{
    public sealed class FakePaymentGateway : IPaymentGateway
    {
        public Task<PaymentResult> ChargeAsync(
            PaymentCommand command,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(command);

            if (command.PaymentMethod == PaymentConstants.Methods.FailingCard)
            {
                return Task.FromResult(new PaymentResult(
                    IsSuccess: false,
                    TransactionId: null,
                    FailureReason: "Card declined"));
            }

            return Task.FromResult(new PaymentResult(
                IsSuccess: true,
                TransactionId: $"txn_{Guid.NewGuid():N}",
                FailureReason: null));
        }
    }
}
