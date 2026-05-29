using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Common.Caching;
using OrderManagementSystem.Common.Outbox;
using OrderManagementSystem.Infrastructure.Configuration;
using OrderManagementSystem.Infrastructure.Payments;
using OrderManagementSystem.Infrastructure.Search;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using StackExchange.Redis;

namespace OrderManagementSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(DatabaseOptions.SectionName))
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.DefaultConnection),
                    "ConnectionStrings:DefaultAzureConnection must be configured")
                .ValidateOnStart();

            services.AddOptions<CacheSettings>()
                .Bind(configuration.GetSection(CacheSettings.SectionName))
                .Validate(
                    options => options.OrderByIdTtlMinutes > 0,
                    "Cache:OrderByIdTtlMinutes must be greater than zero")
                .Validate(
                    options => options.OperationTimeoutMilliseconds > 0,
                    "Cache:OperationTimeoutMilliseconds must be greater than zero")
                .Validate(
                    options => options.CircuitFailureRatio > 0 && options.CircuitFailureRatio <= 1,
                    "Cache:CircuitFailureRatio must be between 0 and 1")
                .Validate(
                    options => options.CircuitMinimumThroughput > 0,
                    "Cache:CircuitMinimumThroughput must be greater than zero")
                .Validate(
                    options => options.CircuitSamplingDurationSeconds > 0,
                    "Cache:CircuitSamplingDurationSeconds must be greater than zero")
                .Validate(
                    options => options.CircuitBreakDurationSeconds > 0,
                    "Cache:CircuitBreakDurationSeconds must be greater than zero")
                .ValidateOnStart();

            services.AddResiliencePipeline("redis-cache-readwrite", (pipelineBuilder, context) =>
            {
                var settings = context.ServiceProvider.GetRequiredService<IOptions<CacheSettings>>().Value;

                pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = settings.CircuitFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(settings.CircuitSamplingDurationSeconds),
                    MinimumThroughput = settings.CircuitMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(settings.CircuitBreakDurationSeconds),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<RedisException>()
                        .Handle<TimeoutRejectedException>()
                })
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(settings.OperationTimeoutMilliseconds)
                });
            });

            services.AddResiliencePipeline("redis-cache-invalidation", (pipelineBuilder, context) =>
            {
                var settings = context.ServiceProvider.GetRequiredService<IOptions<CacheSettings>>().Value;

                pipelineBuilder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(settings.OperationTimeoutMilliseconds)
                });
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "oms:";
            });

            services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<IResilientCacheService, ResilientCacheService>();
            services.AddScoped<ICacheService, RedisCacheService>();

            services.AddScoped<IOutboxWriter, OutboxWriter>();
            services.AddScoped<IOutboxStore, SqlOutBoxStore>();
            services.AddScoped<IOutboxEventProcessor, OutboxEventProcessor>();

            services.Configure<OutboxProcessorOptions>(
                configuration.GetSection(OutboxProcessorOptions.SectionName));
            services.AddHostedService<OutboxProcessorBackgroundService>();

            services.AddScoped<IPaymentGateway, FakePaymentGateway>();

            services.AddOptions<ElasticsearchOptions>()
                .Bind(configuration.GetSection(ElasticsearchOptions.SectionName))
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Uri),
                    "Elasticsearch:Uri must be configured")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.ProductsIndexName),
                    "Elasticsearch:ProductsIndexName must be configured")
                .ValidateOnStart();

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
                var settings = new ElasticsearchClientSettings(new Uri(options.Uri));

                return new ElasticsearchClient(settings);
            });

            services.AddScoped<IProductSearchIndexer, ElasticsearchProductSearchIndexer>();

            return services;
        }
    }
}
