using OrderManagementSystem.Features.Products.Events;
using OrderManagementSystem.Infrastructure.Search;
using System.Text.Json;

namespace OrderManagementSystem.Common.Outbox
{
    public sealed class OutboxEventProcessor : IOutboxEventProcessor
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IProductSearchIndexer _productSearchIndexer;

        public OutboxEventProcessor(IProductSearchIndexer productSearchIndexer)
        {
            ArgumentNullException.ThrowIfNull(productSearchIndexer);

            _productSearchIndexer = productSearchIndexer;
        }

        public async Task ProcessAsync(
             OutboxMessage message,
             CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (message.EventType == OutBoxEventTypes.ProductCreated)
            {
                await ProcessProductCreatedAsync(message, cancellationToken);
                return;
            }

            throw new NotSupportedException(
                $"Unsupported outbox event type: {message.EventType}");
        }

        private async Task ProcessProductCreatedAsync(
            OutboxMessage message,
            CancellationToken cancellationToken)
        {
            var productCreatedEvent = JsonSerializer.Deserialize<ProductCreatedEvent>(
                message.Payload,
                JsonOptions);

            if (productCreatedEvent is null)
            {
                throw new InvalidOperationException(
                    $"Could not deserialize ProductCreated event. OutboxEventId: {message.OutboxEventId}");
            }

            var document = new ProductSearchDocument
            {
                ProductId = productCreatedEvent.ProductId,
                Sku = productCreatedEvent.Sku,
                ProductName = productCreatedEvent.ProductName,
                Price = productCreatedEvent.Price,
                IsActive = productCreatedEvent.IsActive
            };

            await _productSearchIndexer.IndexAsync(document, cancellationToken);
        }
    }

}
