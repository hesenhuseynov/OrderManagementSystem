using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace OrderManagementSystem.Common.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _distributedCache;

        public RedisCacheService(IDistributedCache  distributedCache)
        {
            _distributedCache = distributedCache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key);


            var json = await _distributedCache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrWhiteSpace(json))
            {
                return default; 
            }

            T?  data = JsonSerializer.Deserialize<T>(json);

            return data;
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key);

            await _distributedCache.RemoveAsync(key, cancellationToken);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);

            var json = JsonSerializer.Serialize(value);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };

            await
                 _distributedCache.SetStringAsync(key, json, options, cancellationToken);
        }
    }
}
