using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Common.Outbox;
using OrderManagementSystem.Infrastructure.Payments;
using OrderManagementSystem.Infrastructure.Search;
using System.Data;

namespace OrderManagementSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrasturcture(this IServiceCollection services)
        {
            services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
            services.AddScoped<IOutboxWriter, OutboxWriter>();
            services.AddScoped<IOutboxStore, SqlOutBoxStore>();
            services.AddScoped<IOutboxEventProcessor, OutboxEventProcessor>();

            services.AddScoped<IPaymentGateway, FakePaymentGateway>();

            services.AddOptions<ElasticsearchOptions>()
    .BindConfiguration(ElasticsearchOptions.SectionName)
    .Validate(options => !string.IsNullOrWhiteSpace(options.Uri),
        "Elasticsearch:Uri must be configured")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ProductsIndexName),
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
