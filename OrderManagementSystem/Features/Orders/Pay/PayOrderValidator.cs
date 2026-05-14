using FluentValidation;

namespace OrderManagementSystem.Features.Orders.Pay
{
    public  sealed class PayOrderValidator : AbstractValidator<PayOrderRequest>
    {
        public PayOrderValidator()
        {
            RuleFor(x => x.IdempotencyKey)
                .NotNull()
                .WithMessage("Idempotency key is required.")
                .WithErrorCode("payment.idempotency_key_required")
                .Must(x => x.HasValue && x.Value != Guid.Empty)
                .WithMessage("Idempotency key must be a non-empty GUID.")
                .WithErrorCode("payment.invalid_idempotency_key");

            RuleFor(x => x.PaymentMethod)
                .NotEmpty()
                .WithMessage("Payment method is required.")
                .WithErrorCode("payment.method_required")
                .Must(m => m is
                    PaymentConstants.Methods.Fake or
                    PaymentConstants.Methods.CreditCard or
                    PaymentConstants.Methods.FailingCard)
                .WithMessage("Payment method is not supported.")
                .WithErrorCode("payment.method_not_supported");


            When(x => x.PaymentMethod == PaymentConstants.Methods.CreditCard, () =>
            {
                RuleFor(x => x.CardLast4)
                .NotEmpty()
                .WithMessage("Card last 4 digits are reuired .")
                .WithErrorCode("payment_card_last4_required")
                .Length(4)
                .WithMessage("Card last  4 must  contain excatly 4 digits")
                .WithErrorCode("payment.invalid_card_last4");
            });
           
        }
    }
}
