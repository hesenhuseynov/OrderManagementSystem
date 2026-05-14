namespace OrderManagementSystem.Infrastructure.Search
{
    public class ElasticsearchOptions
    {
        public const string SectionName = "Elasticsearch";

        public string Uri { get; init; } = string.Empty;

        public string ProductsIndexName { get; init; } = "oms-products";
    }
}
