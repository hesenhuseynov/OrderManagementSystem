namespace OrderManagementSystem.Features.Orders
{
    public static class OrderConstants
    {
        public static class Statuses
        {
            public const string Pending = "Pending";
            public const string Cancelled = "Cancelled";
            public const string Paid = "Paid";
        }
        public static class Currency
        {
            public const string Azn = "AZN";
        }
        public static class InventoryMovementTypes
        {
            public const string Reserve = "Reserve";
            public const string Restock = "Restock";
            public const string Deduct = "Deduct";
            public const string Adjust = "Adjust";
        }
        public static class Reasons
        {
            public const string OrderCreated = "Order created";
            public const string OrderCancelled = "Order cancelled";
            public const string OrderPaid = "Order paid";
        }
        public  static  class ChangedBy
        {
            public const string System = "System";
        }
    }
}
