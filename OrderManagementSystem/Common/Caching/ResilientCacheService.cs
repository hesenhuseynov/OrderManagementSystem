using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;
using StackExchange.Redis;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;

namespace OrderManagementSystem.Common.Caching
{
    public sealed class ResilientCacheService : IResilientCacheService
    {
        private const string ReadWritePipelineName = "redis-cache-readwrite";
        private const string InvalidationPipelineName = "redis-cache-invalidation";

        private readonly ICacheService _inner;
        private readonly ResiliencePipeline _readWritePipeline;
        private readonly ResiliencePipeline _invalidationPipeline;
        private readonly ILogger<ResilientCacheService> _logger;

        public ResilientCacheService(
            ICacheService inner,
            ResiliencePipelineProvider<string> pipelineProvider,
            ILogger<ResilientCacheService> logger)
        {
            ArgumentNullException.ThrowIfNull(inner);
            ArgumentNullException.ThrowIfNull(pipelineProvider);
            ArgumentNullException.ThrowIfNull(logger);

            _inner = inner;
            _readWritePipeline = pipelineProvider.GetPipeline(ReadWritePipelineName);
            _invalidationPipeline = pipelineProvider.GetPipeline(InvalidationPipelineName);
            _logger = logger;
        }

        public async Task<CacheResult<T>> TryGetAsync<T>(
        string key,
        CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            try
            {
                var value = await _readWritePipeline.ExecuteAsync(
                    async token =>
                    {
                        return await _inner.GetAsync<T>(key, token);
                    },
                    cancellationToken);

                return value is null
                    ? CacheResult<T>.Miss()
                    : CacheResult<T>.Hit(value);

            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis cache circuit is open. Skipping cache get. CacheKey: {CacheKey}",
                    key);

                return CacheResult<T>.Miss();
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis cache get timed out. Falling back to source of truth. CacheKey: {CacheKey}",
                    key);

                return CacheResult<T>.Miss();
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis cache infrastructure failure during get. Falling back to source of truth. CacheKey: {CacheKey}",
                    key);

                return CacheResult<T>.Miss();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unexpected cache get failure. Falling back to source of truth. CacheKey: {CacheKey}",
                    key);

                return CacheResult<T>.Miss();
            }
        }


        public async Task TrySetAsync<T>(
     string key,
     T value,
     TimeSpan ttl,
     CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);

            try
            {
                await _readWritePipeline.ExecuteAsync(
                    async token =>
                    {
                        await _inner.SetAsync(
                            key,
                            value,
                            ttl,
                            token);
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (BrokenCircuitException ex)
            {
                _logger.LogDebug(
                    ex,
                    "Redis cache circuit is open. Skipping cache set. CacheKey: {CacheKey}",
                    key);
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis cache set timed out. Business flow will continue. CacheKey: {CacheKey}",
                    key);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis cache infrastructure failure during set. Business flow will continue. CacheKey: {CacheKey}",
                    key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unexpected cache set failure. Business flow will continue. CacheKey: {CacheKey}",
                    key);
            }
        }
        public async Task TryRemoveAsync(
      string key,
      CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            try
            {
                await _invalidationPipeline.ExecuteAsync(
                    async token =>
                    {
                        await _inner.RemoveAsync(
                            key,
                            token);
                    },
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TimeoutRejectedException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis cache remove timed out. Stale cache may exist until TTL expires. CacheKey: {CacheKey}",
                    key);
            }
            catch (RedisException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis cache infrastructure failure during remove. Stale cache may exist until TTL expires. CacheKey: {CacheKey}",
                    key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unexpected cache remove failure. Stale cache may exist until TTL expires. CacheKey: {CacheKey}",
                    key);
            }
        }


    }
}
