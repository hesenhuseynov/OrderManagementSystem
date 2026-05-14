namespace OrderManagementSystem.Features.Orders.Pay
{
    public static  class PaymentConstants 
    {
        public static class Methods
        {
            public const string Fake = "Fake";

            public const string CreditCard = "CreditCard";

            public const string FailingCard = "FailingCard";
        }

        public static class Statuses
        {
            public const string Pending = "Pending";
            public const string Succeeded = "Succeeded";
            public const string Failed = "Failed";
        }

        public static class Providers
        {
            public const string Fake = "Fake";
            public const string Stripe = "Stripe";
        }
       
    }
}
