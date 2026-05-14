namespace OrderManagementSystem.Common.Caching
{
    public interface IResilientCacheService
    {
        Task<CacheResult<T>> TryGetAsync<T>(
            string key, CancellationToken cancellationToken
            );

        Task TrySetAsync<T>(
            string key,
            T value,
            TimeSpan ttl,
            CancellationToken cancellationToken
            );

        Task TryRemoveAsync(string key, CancellationToken cancellationToken);
    }
}
