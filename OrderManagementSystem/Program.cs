using Asp.Versioning;
using FluentValidation;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Common.Caching;
using OrderManagementSystem.Common.Outbox;
using OrderManagementSystem.Features.Customers.GetAllCustomer;
using OrderManagementSystem.Features.Customers.GetById;
using OrderManagementSystem.Features.Orders.Cancel;
using OrderManagementSystem.Features.Orders.Create;
using OrderManagementSystem.Features.Orders.GetById;
using OrderManagementSystem.Features.Orders.Pay;
using OrderManagementSystem.Features.Products.Create;
using OrderManagementSystem.Features.Products.Search;
using OrderManagementSystem.Infrastructure;
using OrderManagementSystem.Infrastructure.Configuration;
using OrderManagementSystem.Middleware;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using StackExchange.Redis;
using System.Reflection;

namespace OrderManagementSystem
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddControllers();
            builder.Services.AddSwaggerGen();

            builder.Services.AddOptions<DatabaseOptions>()
                .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
                 .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultConnection),
                 "ConnectionStrings:DefaultAzureConnection must be configured"
                 ).ValidateOnStart();

            builder.Services.AddOptions<CacheSettings>()
             .Bind(builder.Configuration.GetSection(CacheSettings.SectionName))
               .Validate(x => x.OrderByIdTtlMinutes > 0,
                 "Cache:OrderByIdTtlMinutes must be greater than zero")
                  .Validate(x => x.OperationTimeoutMilliseconds > 0,
                 "Cache:OperationTimeoutMilliseconds must be greater than zero")
                  .Validate(x => x.CircuitFailureRatio > 0 && x.CircuitFailureRatio <= 1,
                "Cache:CircuitFailureRatio must be between 0 and 1")
                 .Validate(x => x.CircuitMinimumThroughput > 0,
                 "Cache:CircuitMinimumThroughput must be greater than zero")
                .Validate(x => x.CircuitSamplingDurationSeconds > 0,
                "Cache:CircuitSamplingDurationSeconds must be greater than zero")
                 .Validate(x => x.CircuitBreakDurationSeconds > 0,
                 "Cache:CircuitBreakDurationSeconds must be greater than zero")
                .ValidateOnStart();


            builder.Services.AddResiliencePipeline("redis-cache-readwrite", (pipelineBuilder, context) =>
            {
                var settings = context.ServiceProvider.GetRequiredService<IOptions<CacheSettings>>().Value;

                pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = settings.CircuitFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(settings.CircuitSamplingDurationSeconds),
                    MinimumThroughput = settings.CircuitMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(settings.CircuitBreakDurationSeconds),

                    ShouldHandle = new PredicateBuilder().Handle<RedisException>()
                    .Handle<TimeoutRejectedException>()
                })
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(settings.OperationTimeoutMilliseconds)
                });
            });

            builder.Services.AddResiliencePipeline("redis-cache-invalidation", (pipelineBuilder, context) =>
            {
                var settings = context.ServiceProvider.GetRequiredService<IOptions<CacheSettings>>().Value;

                pipelineBuilder.AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromMilliseconds(settings.OperationTimeoutMilliseconds)
                });

            });

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
                options.InstanceName = "oms:";
            });

            builder.Services.AddScoped<IResilientCacheService, ResilientCacheService>();
            builder.Services.AddScoped<ICacheService, RedisCacheService>();

            builder.Services.AddScoped<GetAllCustomersHandler>();
            builder.Services.AddScoped<GetCustomerByIdHandler>();
            builder.Services.AddScoped<CreateOrderHandler>();
            builder.Services.AddScoped<GetOrderByIdHandler>();
            builder.Services.AddScoped<CancelOrderHandler>();
            builder.Services.AddScoped<CreateProductHandler>();
            builder.Services.AddScoped<SearchProductsHandler>();
            builder.Services.AddScoped<PayOrderHandler>();
            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            

            builder.Services.Configure<OutboxProcessorOptions>(
                 builder.Configuration.GetSection(OutboxProcessorOptions.SectionName));

              builder.Services.AddInfrasturcture();


            builder.Services.AddHostedService<OutboxProcessorBackgroundService>();

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
            }).AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            }); 




            var app = builder.Build();

            app.UseGlobalExcpetionMiddleware();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.MapControllers();  

            app.Run();
        }
    }
}
