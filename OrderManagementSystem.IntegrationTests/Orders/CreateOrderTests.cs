using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using OrderManagementSystem.Common.Caching;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OrderManagementSystem.IntegrationTests.Orders
{ 

    [Collection("IntegrationTests")]
    public class CreateOrderTests
    {

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IntegrationTestFixture _fixture;

        public CreateOrderTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CreateOrder_Should_Create_Order_Decrease_Stock_Warm_Cache()
        {

            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();

            var customerId = await _fixture.InsertCustomerAsync();
            var productId = await _fixture.InsertProductAsync();


            var request = new
            {
                customerId,
                items = new[]
                {
                    new
                    {
                        productId,
                        quantity=2
                    }
                }
            };

            var response = await _fixture.Client.PostAsJsonAsync("/api/v1/orders", request);

            // Assert HTTP
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("CREATE ORDER RESPONSE:");
            Console.WriteLine(json);

            var body = System.Text.Json.JsonSerializer.Deserialize<CreateOrderResponseDto>(
                json,
                jsonOptions);


            body.Should().NotBeNull();
            body!.OrderId.Should().BeGreaterThan(0);
            body.CustomerId.Should().Be(customerId);
            body.Status.Should().Be("Pending");
            body.Currency.Should().Be("AZN");
            body.TotalAmount.Should().Be(400m);
            // Assert DB
            using var connection = _fixture.CreateConnection();
            connection.Open();

            var dbOrder = await connection.QuerySingleOrDefaultAsync<OrderRow>(
                """
            SELECT OrderId, CustomerId, Status, Currency, TotalAmount
            FROM dbo.Orders
            WHERE OrderId = @OrderId
            """,
                new { OrderId = body.OrderId });

            dbOrder.Should().NotBeNull();
            dbOrder!.CustomerId.Should().Be(customerId);
            dbOrder.Status.Should().Be("Pending");
            dbOrder.Currency.Should().Be("AZN");
            dbOrder.TotalAmount.Should().Be(400m);

            var orderItemsCount = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM dbo.OrderItems WHERE OrderId = @OrderId",
                new { OrderId = body.OrderId });

            orderItemsCount.Should().Be(1);

            var stock = await connection.QuerySingleAsync<int>(
                "SELECT StockQuantity FROM dbo.Products WHERE ProductId = @ProductId",
                new { ProductId = productId });

            stock.Should().Be(3);

            using var scope = _fixture.Factory.Services.CreateScope();

            var distributedCache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

            var cacheKey = CacheKeys.OrderById(body.OrderId);

            var cacheValue = await distributedCache.GetStringAsync(cacheKey);

            cacheValue.Should().NotBeNullOrWhiteSpace();

        }


        [Fact]
        public async Task CreateOrder_Should_Return_Conflict_When_Stock_Is_Insufficient()
        {
            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();

            var customerId = await _fixture.InsertCustomerAsync();
            var productId = await _fixture.InsertProductAsync(
                productName: "Keyboard",
                price: 2000,
                stockQuantity: 1
                );
            var request = new
            {
                customerId,
                items = new[]
                {
                    new
                    {
                        productId,
                        quantity  =2
                    }

               }
            };

            var response = await _fixture.Client.PostAsJsonAsync("/api/v1/orders", request);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);

            using var connection = _fixture.CreateConnection();
            connection.Open();

            var ordersCount = await connection.QuerySingleAsync<int>("SELECT COUNT (*) FROM dbo.Orders");

            ordersCount.Should().Be(0);

            var oderItemsCount = await connection.QuerySingleAsync<int>(
                "SELECT  COUNT(*) FROM dbo.OrderItems"
                );
            oderItemsCount.Should().Be(0);

            var stock = await connection.QuerySingleAsync<int>(
    "SELECT StockQuantity FROM dbo.Products WHERE ProductId = @ProductId",
    new { ProductId = productId });
            stock.Should().Be(1);

        }

        [Fact]
        public async Task CreateOrder_Should_Return_NotFound_When_Customer_Does_Not_Exists()
        {
            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();

            var productId = await _fixture.InsertProductAsync(
                "TestProductGalaxy", 300, 15
                );

            var missingCustomerId = 999999;

            var request = new
            {
                customerId = missingCustomerId,
                items = new[]
                {
                    new
                    {
                        productId,
                        quantity = 3
                    }
                }
            };

            var response = await _fixture.Client.PostAsJsonAsync("/api/v1/orders", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var responseBody = await response.Content.ReadAsStringAsync();

            responseBody.Should().Contain("Customer");

            using var connection = _fixture.CreateConnection();

            connection.Open();

            var ordersCount = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*)  FROM dbo.Orders"
                );

            ordersCount.Should().Be(0);

            var orderItemsCount = await connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM dbo.OrderItems");

            orderItemsCount.Should().Be(0);

            var stock = await connection.QuerySingleAsync<int>(

                "SELECT StockQuantity FROM dbo.Products WHERE ProductId=@ProductId",
                new { ProductId = productId }
                );
            stock.Should().Be(15);
        }

        [Fact]
        public async Task CreteOrder_Should_Return_NotFound_When_Product_Does_Not_Exists()
        {
            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();

            var customerId = await _fixture.InsertCustomerAsync();
            var missingProductId = 9999;

            var request = new
            {
                customerId,
                items = new[]
                {
                    new
                    {
                        productId= missingProductId,
                        quantity =2
                    }
                }
            };

            var response = await _fixture.Client.PostAsJsonAsync("/api/v1/orders/", request);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var resposneBody = await  response.Content.ReadAsStringAsync();

            resposneBody.Should().Contain("order.product_not_found");

            using var conneciton = _fixture.CreateConnection();
            conneciton.Open();

            var ordersCount =  await conneciton.QuerySingleAsync<int> ("""
                SELECT COUNT(*)  
                FROM dbo.Orders 
                """);
            ordersCount.Should().Be(0);

            var ordersItemsCount = await conneciton.QuerySingleAsync<int>(
                 """
                   SELECT COUNT(*) From dbo.OrderItems 
                  """

                );

            ordersItemsCount.Should().Be(0);

            var inventoryMovementsCount = await conneciton.QuerySingleAsync<int>(
                """
                 SELECT COUNT(*) FROM dbo.InventoryMovements
                """
                );
            inventoryMovementsCount.Should().Be(0);

            var orderStatusHistoryCount = await conneciton.QuerySingleAsync<int>(
                """
                SELECT COUNT(*) FROM dbo.OrderStatusHistory
               """
                );

            orderStatusHistoryCount.Should().Be(0);

        }


        private sealed record CreateOrderResponseDto(
 int OrderId,
 string OrderNumber,
 int CustomerId,
 DateTime OrderDate,
 string Status,
 string Currency,
 decimal TotalAmount);

        private sealed class OrderRow
        {
            public int OrderId { get; init; }
            public int CustomerId { get; init; }
            public string Status { get; init; } = string.Empty;
            public string Currency { get; init; } = string.Empty;
            public decimal TotalAmount { get; init; }
        }
    }  
}

