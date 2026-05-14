using Dapper;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OrderManagementSystem.IntegrationTests.Products
{
    [Collection("IntegrationTests")]
    public sealed class CreateProductTests
    {

        private static readonly JsonSerializerOptions jsonOptions = new()
        {

            PropertyNameCaseInsensitive = true
        };

        private readonly IntegrationTestFixture _fixture;
        public CreateProductTests(IntegrationTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateProduct_Should_Insert_Product_And_OutBoxEvent()
        {
            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();

            var request = new
            {
                sku = $"SKU-{Guid.NewGuid():N}",
                productName = "Mechanical Keyboard",
                price = 250m,
                stockQuantity = 10
            };

            var response = await _fixture.Client.PostAsJsonAsync("/api/v1/products", request);

            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var json = await response.Content.ReadAsStringAsync();

            var body = JsonSerializer.Deserialize<CreateProductResponseDto>(json, jsonOptions);

            body.Should().NotBeNull();
            body!.ProductId.Should().BeGreaterThan(0);
            body.Sku.Should().Be(request.sku);
            body.ProductName.Should().Be(request.productName);
            body.Price.Should().Be(request.price);
            body.StockQuantity.Should().Be(request.stockQuantity);
            body.IsActive.Should().BeTrue();

            using var connection = _fixture.CreateConnection();
            connection.Open();

            var product = await connection.QuerySingleOrDefaultAsync<ProductRow>(
            """
            SELECT
                ProductId,
                Sku,
                ProductName,
                Price,
                StockQuantity,
                IsActive
            FROM dbo.Products
            WHERE ProductId = @ProductId;
            """,
            new
            {
                body.ProductId
            });

            product.Should().NotBeNull();
            product!.ProductId.Should().Be(body.ProductId);
            product.Sku.Should().Be(request.sku);
            product.ProductName.Should().Be(request.productName);
            product.Price.Should().Be(request.price);
            product.StockQuantity.Should().Be(request.stockQuantity);
            product.IsActive.Should().BeTrue();

            var outboxEvent = await connection.QuerySingleOrDefaultAsync<OutboxEventRow>(
                """
            SELECT
                EventId,
                EventType,
                AggregateType,
                AggregateId,
                Payload,
                ProcessedAt,
                RetryCount
            FROM dbo.OutboxEvents
            WHERE AggregateType = 'Product'
              AND AggregateId = @AggregateId;
            """,
                new
                {
                    AggregateId = body.ProductId.ToString()
                });

            outboxEvent.Should().NotBeNull();

            outboxEvent!.EventId.Should().NotBe(Guid.Empty);
            outboxEvent.EventType.Should().Be("ProductCreated");
            outboxEvent.AggregateType.Should().Be("Product");
            outboxEvent.AggregateId.Should().Be(body.ProductId.ToString());
            outboxEvent.ProcessedAt.Should().BeNull();
            outboxEvent.RetryCount.Should().Be(0);
            outboxEvent.Payload.Should().NotBeNullOrWhiteSpace();

            var payload = JsonSerializer.Deserialize<ProductCreatedPayloadDto>(
            outboxEvent.Payload,
          jsonOptions);

            payload.Should().NotBeNull();
            payload!.ProductId.Should().Be(body.ProductId);
            payload.Sku.Should().Be(body.Sku);
            payload.ProductName.Should().Be(body.ProductName);
            payload.Price.Should().Be(body.Price);
            payload.IsActive.Should().BeTrue();

        }



        [Fact]
        public async Task CreateProduct_Should_Return_Conflict_When_Sku_Already_Exists()
        {
            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();
            var sku = $"SKU-{Guid.NewGuid():N}";
            var firstRequest = new
            {
                sku,
                productName = "Mechanical Keyboard",
                price = 250m,
                stockQuantity = 10
            };

            var secondRequest = new
            {
                sku,
                productName = " Other Keyboard",
                price = 300m,
                stockQuantity = 5
            };

            var firstResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/products", firstRequest);


            firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var  secondResponse= await _fixture.Client.PostAsJsonAsync("/api/v1/products", secondRequest);

            var secondJson  = await secondResponse.Content.ReadAsStringAsync();

            secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);


            using var connection = _fixture.CreateConnection();
            connection.Open();

            var productsCount = await connection.QuerySingleAsync<int>(
                """
                SELECT COUNT(*) 
                From dbo.Products  
                Where Sku=@Sku 
                """, new {Sku=sku}
                );

            productsCount.Should().Be(1);

            var outboxEventsCount = await connection.QuerySingleAsync<int>(
                """
                SELECT COUNT(*)  
                From dbo.OutboxEvents
                WHERE  EventType='ProductCreated' 
                AND AggregateType='Product' 
                """
                );

            outboxEventsCount.Should().Be(1);
        }




        private sealed record CreateProductResponseDto(
            int ProductId, string Sku, string ProductName, decimal Price, int StockQuantity, bool IsActive
            );


        private sealed record ProductCreatedPayloadDto(
            int ProductId, string Sku, string ProductName, decimal Price, bool IsActive
            );

        private sealed class ProductRow
        {
            public int ProductId { get; init; }
            public string Sku { get; init; } = string.Empty;
            public string ProductName { get; init; } = string.Empty;
            public decimal Price { get; init; }
            public int StockQuantity { get; init; }
            public bool IsActive { get; init; }
        }


        private sealed class OutboxEventRow
        {
            public Guid EventId { get; init; }
            public string EventType { get; init; } = string.Empty;
            public string AggregateType { get; init; } = string.Empty;
            public string AggregateId { get; init; } = string.Empty;
            public string Payload { get; init; } = string.Empty;
            public DateTime? ProcessedAt { get; init; }
            public int RetryCount { get; init; }
        }
    }
}


