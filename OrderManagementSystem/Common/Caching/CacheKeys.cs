namespace OrderManagementSystem.Common.Caching
{
    public static class CacheKeys
    {
        public static string OrderById(int orderId) => $"order:{orderId}";
    }
}
