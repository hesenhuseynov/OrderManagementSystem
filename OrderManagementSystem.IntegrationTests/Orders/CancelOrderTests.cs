using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using OrderManagementSystem.Common.Caching;
using OrderManagementSystem.Features.Orders.Cancel;
using OrderManagementSystem.Features.Orders.Create;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace OrderManagementSystem.IntegrationTests.Orders
{
    [Collection("IntegrationTests")]
    public sealed class CancelOrderTests
    {

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IntegrationTestFixture _fixture;
        public CancelOrderTests(IntegrationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task CancelOrder_Should_Cancel_Order_Restore_Stock_And_Invalidate_Cache()
        {
            await _fixture.FlushRedisAsync();
            await _fixture.ResetDatabaseAsync();

            var customerId = await _fixture.InsertCustomerAsync();
            var productId = await _fixture.InsertProductAsync(
                productName: "Keyboard",
                price: 200,
                stockQuantity: 5
                );

            var createRequest = new
            {
                customerId,
                Items = new[]
                {
                    new
                    {
                        productId,
                        quantity = 2
                    }
                }
            };

            var createdResponse = await _fixture.Client.PostAsJsonAsync("/api/v1/orders", createRequest);

            createdResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);

            var json = await createdResponse.Content.ReadAsStringAsync();

            CreateOrderResponseDto? createOrder = JsonSerializer.Deserialize<CreateOrderResponseDto>(json, JsonOptions);

            createOrder.Should().NotBeNull();
            createOrder!.OrderId.Should().BeGreaterThan(0);

            using var connection = _fixture.CreateConnection();
            connection.Open();

            var stockAfterCreate = await connection.QuerySingleAsync<int>(
                "SELECT StockQuantity FROM dbo.Products WHERE ProductId=@ProductId"
               , new { ProductId = productId });

            stockAfterCreate.Should().Be(3);

            using var scopeBeforeCancel = _fixture.Factory.Services.CreateScope();
            var distributedCacheBeforeCancel = scopeBeforeCancel.ServiceProvider.GetRequiredService<IDistributedCache>();

            var cacheKey = CacheKeys.OrderById(createOrder.OrderId);
            var cacheBeforeCancel = await distributedCacheBeforeCancel.GetStringAsync(cacheKey);
            cacheBeforeCancel.Should().NotBeNullOrWhiteSpace();

            var cancelResponse = await _fixture.Client.PostAsync(
    $"/api/v1/orders/{createOrder.OrderId}/cancel",
    content: null);

            cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var cancelJson = await cancelResponse.Content.ReadAsStringAsync();

            CancelOrderResponse? cancelBody = JsonSerializer.Deserialize<CancelOrderResponse>(cancelJson, JsonOptions);

            cancelBody.Should().NotBeNull();

            cancelBody!.OrderId.Should().Be(createOrder.OrderId);
            cancelBody.Status.Should().Be("Cancelled");

            var orderStatus = await connection.QuerySingleAsync<string>(
                "SELECT Status FROM  dbo.Orders   Where OrderId=@OrderId",
                new { OrderId = createOrder.OrderId }
                );

            orderStatus.Should().Be("Cancelled");

            var stockAfterCancel = await connection.QuerySingleAsync<int>(
                "SELECT StockQuantity  FROM dbo.Products Where ProductId =@ProductId", new { ProductId = productId }
                );

            stockAfterCancel.Should().Be(5);

            using var scopeAfterCancel = _fixture.Factory.Services.CreateScope();

            var distributedCacheAfterCancel = scopeAfterCancel.ServiceProvider.GetRequiredService<IDistributedCache>();
            var cacheAfterCancel = await distributedCacheAfterCancel.GetStringAsync(cacheKey);

            cacheAfterCancel.Should().BeNull();
        }

        [Fact]
        public async Task CancelOrder_Should_Return_Conflict_When_Order_Already_Cancelled()
        {
            await _fixture.FlushRedisAsync();
            await _fixture.ResetDatabaseAsync();

            int  customerId = await  _fixture.InsertCustomerAsync();

            int  productId =  await _fixture.InsertProductAsync(
                productName: "Keyboard",
                price: 200,
                stockQuantity: 5
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

            HttpResponseMessage createResponse = await _fixture.Client.PostAsJsonAsync(
                "/api/v1/orders", request);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createJson = await createResponse.Content.ReadAsStringAsync();

            var  createOrder = JsonSerializer.Deserialize<CreateOrderResponse>(createJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            createOrder.Should().NotBeNull();
            createOrder!.OrderId.Should().BeGreaterThan(0);

            var firstCancelResponse = await _fixture.Client.PostAsync(
                $"/api/v1/orders/{createOrder.OrderId}/cancel", content: null
                );

            firstCancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var secondCancelResponse = await _fixture.Client.PostAsync($"/api/v1/orders/{createOrder.OrderId}/cancel"
                 , content: null
                );


            secondCancelResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

            using var connection = _fixture.CreateConnection();
            connection.Open();

            var orderStatus = await connection.QuerySingleAsync<string>(
                """
                SELECT Status  
                From dbo.Orders  
                WHERE OrderId =@OrderId  
                """, new { OrderId = createOrder.OrderId });

            orderStatus.Should().Be("Cancelled");

            var stockAfterSecondCancel = await connection.QuerySingleAsync<int>(

                """
                  SELECT  StockQuantity 
                  FROM dbo.Products  
                  WHERE ProductId =@ProductId 
                """
                , new {ProductId=productId});

            stockAfterSecondCancel.Should().Be(5);

            var restockMovementCount = await connection.QuerySingleAsync<int>(
         """
        SELECT COUNT(*)
        FROM dbo.InventoryMovements
        WHERE OrderId = @OrderId
          AND ProductId = @ProductId
          AND MovementType = 'Restock'
        """,
      new { OrderId=createOrder.OrderId,ProductId=productId});

            restockMovementCount.Should().Be(1);

        }
        
        private sealed record CreateOrderResponseDto(
    int OrderId,
    string OrderNumber,
    int CustomerId,
    DateTime OrderDate,
    string Status,
    string Currency,
    decimal TotalAmount);

        private sealed record CancelOrderResponseDto(
 int OrderId,
 string Status);
    }

}
