using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;

namespace OrderManagementSystem.Infrastructure.Search
{
    public sealed class ElasticsearchProductSearchIndexer:IProductSearchIndexer
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticsearchOptions _options;
        public ElasticsearchProductSearchIndexer(
           ElasticsearchClient client,
           IOptions<ElasticsearchOptions> options)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);

            _client = client;
            _options = options.Value;
        }

        public async  Task IndexAsync(ProductSearchDocument document, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(document);

            var response = await _client.IndexAsync(
                document,
                index: _options.ProductsIndexName,
                id: document.ProductId,
                cancellationToken: cancellationToken);

            if (!response.IsValidResponse)
            {
                throw new InvalidOperationException(
                    $"Failed to index product document. ProductId: {document.ProductId}. DebugInformation: {response.DebugInformation}");
            }
        }
    }
}
