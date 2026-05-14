using OrderManagementSystem.Common.Errors;

namespace OrderManagementSystem.Features.Orders.Pay
{
    public static class PaymentErrors
    {
        public static Error OrderNotFound(int orderId) =>
             Error.NotFound(
                 "order.not_found",
                 $"Order with id '{orderId}' was not found.");

        public static Error OrderCannotBePaid(int orderId, string status) =>
            Error.Conflict(
                "order.cannot_be_paid",
                $"Order '{orderId}' cannot be paid because current status is '{status}'.");

        public static Error PaymentDeclined(string reason) =>
            Error.Conflict(
                "payment.declined",
                reason);

        public static Error IdempotencyKeyAlreadyUsedForAnotherOrder() =>
            Error.Conflict(
                "payment.idempotency_key_conflict",
                "Idempotency key was already used for another order.");

        public static Error PaymentAlreadyFailed() =>
            Error.Conflict(
                "payment.already_failed",
                "Payment with the same idempotency key has already failed.");
    }
}
