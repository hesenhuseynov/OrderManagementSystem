using Elastic.Clients.Elasticsearch;
using FluentValidation;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Common.Results;
using OrderManagementSystem.Common.Validation;
using OrderManagementSystem.Infrastructure.Search;
using Result = OrderManagementSystem.Common.Results.Result;

namespace OrderManagementSystem.Features.Products.Search
{
    public sealed class SearchProductsHandler
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticsearchOptions _options;
        private readonly IValidator<SearchProductsRequest> _validator;
        public SearchProductsHandler(
              ElasticsearchClient client,
              IOptions<ElasticsearchOptions> options,
              IValidator<SearchProductsRequest> validator)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(validator);

            _client = client;
            _options = options.Value;
            _validator = validator;
        }

        public async Task<Result<IReadOnlyList<SearchProductResponse>>> HandleAsync(SearchProductsRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToErrorList();

                return Result.Failure<IReadOnlyList<SearchProductResponse>>(errors);
            }

            var response = await _client.SearchAsync<ProductSearchDocument>(
                 s => s
                     .Indices(_options.ProductsIndexName)
                     .Size(20)
                     .Query(q => q
                         .MultiMatch(mm => mm
                             .Query(request.Query)
                             .Fields(new[] { "productName", "sku" })
                         )
                     ),
                 cancellationToken);

            if (!response.IsValidResponse)
            {
                throw new InvalidOperationException(
                    $"Product search failed. DebugInformation: {response.DebugInformation}");
            }

            var products = response.Documents 
                .Select(x => new SearchProductResponse(
                    x.ProductId,
                    x.Sku,
                    x.ProductName,
                    x.Price,
                    x.IsActive))
                .ToList();

            return Result.Success<IReadOnlyList<SearchProductResponse>>(products);
        }
    }

}