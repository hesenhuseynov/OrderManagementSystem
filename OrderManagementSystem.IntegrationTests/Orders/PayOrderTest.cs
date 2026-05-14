using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using FluentAssertions;

namespace OrderManagementSystem.IntegrationTests.Orders
{
    [Collection("IntegrationTests")]
    public sealed class PayOrderTests
    {
        private readonly IntegrationTestFixture _fixture;

        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PayOrderTests(IntegrationTestFixture fixture)
        {
            ArgumentNullException.ThrowIfNull(fixture);

            _fixture = fixture;
        }

        [Fact]
        public async Task PayOrder_Should_Mark_Order_As_Paid_Insert_Payment_And_Write_OutboxEvent()
        {
            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();

            var customerId = await _fixture.InsertCustomerAsync();

            var productId = await _fixture.InsertProductAsync(
                productName: "Gaming Keyboard",
                price: 250m,
                stockQuantity: 10);

            var createOrderRequest = new
            {
                customerId,
                items = new[]
                {
                    new
                    {
                        productId,
                        quantity = 2
                    }
                }
            };

            var createOrderResponse = await _fixture.Client.PostAsJsonAsync(
                "/api/v1/orders",
                createOrderRequest);

            createOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createOrderJson = await createOrderResponse.Content.ReadAsStringAsync();

            var createdOrder = JsonSerializer.Deserialize<CreateOrderResponseDto>(
                createOrderJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            createdOrder.Should().NotBeNull();
            createdOrder!.OrderId.Should().BeGreaterThan(0);
            createdOrder.Status.Should().Be("Pending");

            var idempotencyKey = Guid.NewGuid();

            var payOrderRequest = new
            {
                paymentMethod = "Fake",
                cardLast4 = (string?)null,
                idempotencyKey
            };

            var payResponse = await _fixture.Client.PostAsJsonAsync(
                $"/api/v1/orders/{createdOrder.OrderId}/pay",
                payOrderRequest);

            var payJson = await payResponse.Content.ReadAsStringAsync();

            Console.WriteLine("PAY ORDER STATUS:");
            Console.WriteLine(payResponse.StatusCode);

            Console.WriteLine("PAY ORDER RESPONSE:");
            Console.WriteLine(payJson);

            payResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var payBody = JsonSerializer.Deserialize<PayOrderResponseDto>(
                payJson,jsonOptions);

            payBody.Should().NotBeNull();
            payBody!.OrderId.Should().Be(createdOrder.OrderId);
            payBody.OrderStatus.Should().Be("Paid");
            payBody.PaymentStatus.Should().Be("Succeeded");
            payBody.PaymentId.Should().BeGreaterThan(0);
            payBody.ExternalTransactionId.Should().NotBeNullOrWhiteSpace();

            using var connection = _fixture.CreateConnection();
            connection.Open();

            var order = await connection.QuerySingleAsync<OrderRow>(
                """
                SELECT
                    OrderId,
                    Status,
                    TotalAmount,
                    Currency
                FROM dbo.Orders
                WHERE OrderId = @OrderId;
                """,
                new
                {
                    OrderId = createdOrder.OrderId
                });

            order.OrderId.Should().Be(createdOrder.OrderId);
            order.Status.Should().Be("Paid");
            order.TotalAmount.Should().Be(500m);
            order.Currency.Should().Be("AZN");

            var payment = await connection.QuerySingleAsync<PaymentRow>(
                """
                SELECT
                    PaymentId,
                    OrderId,
                    Provider,
                    Status,
                    Amount,
                    Currency,
                    IdempotencyKey,
                    ExternalTransactionId,
                    FailureReason
                FROM dbo.Payments
                WHERE OrderId = @OrderId;
                """,
                new
                {
                    OrderId = createdOrder.OrderId
                });

            payment.PaymentId.Should().Be(payBody.PaymentId);
            payment.OrderId.Should().Be(createdOrder.OrderId);
            payment.Provider.Should().Be("Fake");
            payment.Status.Should().Be("Succeeded");
            payment.Amount.Should().Be(500m);
            payment.Currency.Should().Be("AZN");
            payment.IdempotencyKey.Should().Be(idempotencyKey);
            payment.ExternalTransactionId.Should().Be(payBody.ExternalTransactionId);
            payment.FailureReason.Should().BeNull();

            var history = await connection.QuerySingleAsync<OrderStatusHistoryRow>(
                """
                SELECT TOP (1)
                    OrderId,
                    OldStatus,
                    NewStatus,
                    Reason
                FROM dbo.OrderStatusHistory
                WHERE OrderId = @OrderId
                  AND OldStatus = 'Pending'
                  AND NewStatus = 'Paid'
                ORDER BY ChangedAt DESC;
                """,
                new
                {
                    OrderId = createdOrder.OrderId
                });

            history.OrderId.Should().Be(createdOrder.OrderId);
            history.OldStatus.Should().Be("Pending");
            history.NewStatus.Should().Be("Paid");
            history.Reason.Should().Be("Order paid");

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
                WHERE EventType = 'PaymentCompleted'
                  AND AggregateType = 'Order'
                  AND AggregateId = @AggregateId;
                """,
                new
                {
                    AggregateId = createdOrder.OrderId.ToString()
                });

            outboxEvent.Should().NotBeNull();
            outboxEvent!.EventId.Should().NotBe(Guid.Empty);
            outboxEvent.EventType.Should().Be("PaymentCompleted");
            outboxEvent.AggregateType.Should().Be("Order");
            outboxEvent.AggregateId.Should().Be(createdOrder.OrderId.ToString());
            outboxEvent.Payload.Should().Contain(createdOrder.OrderId.ToString());
            outboxEvent.Payload.Should().Contain(payBody.PaymentId.ToString());
            outboxEvent.Payload.Should().Contain(payBody.ExternalTransactionId!);

            outboxEvent.ProcessedAt.Should().BeNull();
            outboxEvent.RetryCount.Should().Be(0);
        }


        [Fact]
        public async Task PayOrder_Should_Be_Idempotent_When_Same_IdempotencyKey_Is_Used()
        {
            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();

            var customerId = await _fixture.InsertCustomerAsync();

            var productId = await _fixture.InsertProductAsync(
                productName: "Gaming Mouse",
                price: 120m,
                stockQuantity: 10);

            var createOrderRequest = new
            {
                customerId,
                items = new[]
                {
            new
            {
                productId,
                quantity = 1
            }
        }
            };

            var createOrderResponse = await _fixture.Client.PostAsJsonAsync(
                "/api/v1/orders",
                createOrderRequest);

            createOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createOrderJson = await createOrderResponse.Content.ReadAsStringAsync();

            var createdOrder = JsonSerializer.Deserialize<CreateOrderResponseDto>(
                createOrderJson,jsonOptions);

            createdOrder.Should().NotBeNull();

            var idempotencyKey = Guid.NewGuid();

            var payOrderRequest = new
            {
                paymentMethod = "Fake",
                cardLast4 = (string?)null,
                idempotencyKey
            };

            var firstPayResponse = await _fixture.Client.PostAsJsonAsync(
                $"/api/v1/orders/{createdOrder!.OrderId}/pay",
                payOrderRequest);

            var firstPayJson = await firstPayResponse.Content.ReadAsStringAsync();

            firstPayResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var firstPayBody = JsonSerializer.Deserialize<PayOrderResponseDto>(
                firstPayJson,jsonOptions);

            firstPayBody.Should().NotBeNull();
            firstPayBody!.PaymentStatus.Should().Be("Succeeded");

            var secondPayResponse = await _fixture.Client.PostAsJsonAsync(
                $"/api/v1/orders/{createdOrder.OrderId}/pay",
                payOrderRequest);

            var secondPayJson = await secondPayResponse.Content.ReadAsStringAsync();

            Console.WriteLine("SECOND PAY STATUS:");
            Console.WriteLine(secondPayResponse.StatusCode);

            Console.WriteLine("SECOND PAY RESPONSE:");
            Console.WriteLine(secondPayJson);

            secondPayResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var secondPayBody = JsonSerializer.Deserialize<PayOrderResponseDto>(
                secondPayJson,jsonOptions);

            secondPayBody.Should().NotBeNull();
            secondPayBody!.OrderId.Should().Be(createdOrder.OrderId);
            secondPayBody.PaymentId.Should().Be(firstPayBody.PaymentId);
            secondPayBody.ExternalTransactionId.Should().Be(firstPayBody.ExternalTransactionId);
            secondPayBody.PaymentStatus.Should().Be("Succeeded");
            secondPayBody.OrderStatus.Should().Be("Paid");

            using var connection = _fixture.CreateConnection();
            connection.Open();

            var paymentCount = await connection.QuerySingleAsync<int>(
                """
        SELECT COUNT(*)
        FROM dbo.Payments
        WHERE OrderId = @OrderId;
        """,
                new
                {
                    OrderId = createdOrder.OrderId
                });

            paymentCount.Should().Be(1);

            var outboxCount = await connection.QuerySingleAsync<int>(
                """
        SELECT COUNT(*)
        FROM dbo.OutboxEvents
        WHERE EventType = 'PaymentCompleted'
          AND AggregateType = 'Order'
          AND AggregateId = @AggregateId;
        """,
                new
                {
                    AggregateId = createdOrder.OrderId.ToString()
                });

            outboxCount.Should().Be(1);
        }

        [Fact]
        public async Task PayOrder_Should_Return_Conflict_When_IdempotencyKey_Is_Used_For_Another_Order()
        {
            // Arrange
            await _fixture.ResetDatabaseAsync();
            await _fixture.FlushRedisAsync();

            var customerId = await _fixture.InsertCustomerAsync();

            var productId = await _fixture.InsertProductAsync(
                productName: "Gaming Headset",
                price: 300m,
                stockQuantity: 10);

            var firstOrderResponse = await _fixture.Client.PostAsJsonAsync(
                "/api/v1/orders",
                new
                {
                    customerId,
                    items = new[]
                    {
                new
                {
                    productId,
                    quantity = 1
                }
                    }
                });

            firstOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var firstOrderJson = await firstOrderResponse.Content.ReadAsStringAsync();

            var firstOrder = JsonSerializer.Deserialize<CreateOrderResponseDto>(
                firstOrderJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            firstOrder.Should().NotBeNull();

            var secondOrderResponse = await _fixture.Client.PostAsJsonAsync(
                "/api/v1/orders",
                new
                {
                    customerId,
                    items = new[]
                    {
                new
                {
                    productId,
                    quantity = 1
                }
                    }
                });

            secondOrderResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var secondOrderJson = await secondOrderResponse.Content.ReadAsStringAsync();

            var secondOrder = JsonSerializer.Deserialize<CreateOrderResponseDto>(
                secondOrderJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            secondOrder.Should().NotBeNull();

            var idempotencyKey = Guid.NewGuid();

            var payOrderRequest = new
            {
                paymentMethod = "Fake",
                cardLast4 = (string?)null,
                idempotencyKey
            };

            var firstPayResponse = await _fixture.Client.PostAsJsonAsync(
                $"/api/v1/orders/{firstOrder!.OrderId}/pay",
                payOrderRequest);

            firstPayResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var secondPayResponse = await _fixture.Client.PostAsJsonAsync(
                $"/api/v1/orders/{secondOrder!.OrderId}/pay",
                payOrderRequest);

            var secondPayJson = await secondPayResponse.Content.ReadAsStringAsync();

            Console.WriteLine("CONFLICT PAY STATUS:");
            Console.WriteLine(secondPayResponse.StatusCode);

            Console.WriteLine("CONFLICT PAY RESPONSE:");
            Console.WriteLine(secondPayJson);

            secondPayResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
            secondPayJson.Should().Contain("payment.idempotency_key_conflict");

            using var connection = _fixture.CreateConnection();
            connection.Open();

            var paymentsCount = await connection.QuerySingleAsync<int>(
                """
        SELECT COUNT(*)
        FROM dbo.Payments;
        """);

            paymentsCount.Should().Be(1);

            var secondOrderStatus = await connection.QuerySingleAsync<string>(
                """
        SELECT Status
        FROM dbo.Orders
        WHERE OrderId = @OrderId;
        """,
                new
                {
                    OrderId = secondOrder.OrderId
                });

            secondOrderStatus.Should().Be("Pending");
        }


        private sealed record CreateOrderResponseDto(
            int OrderId,
            string OrderNumber,
            int CustomerId,
            DateTime OrderDate,
            string Status,
            string Currency,
            decimal TotalAmount);

        private sealed record PayOrderResponseDto(
            int OrderId,
            string OrderStatus,
            int PaymentId,
            string PaymentStatus,
            string? ExternalTransactionId,
            string Message);

        private sealed class OrderRow
        {
            public int OrderId { get; init; }
            public string Status { get; init; } = string.Empty;
            public decimal TotalAmount { get; init; }
            public string Currency { get; init; } = string.Empty;
        }

        private sealed class PaymentRow
        {
            public int PaymentId { get; init; }
            public int OrderId { get; init; }
            public string Provider { get; init; } = string.Empty;
            public string Status { get; init; } = string.Empty;
            public decimal Amount { get; init; }
            public string Currency { get; init; } = string.Empty;
            public Guid IdempotencyKey { get; init; }
            public string? ExternalTransactionId { get; init; }
            public string? FailureReason { get; init; }
        }

        private sealed class OrderStatusHistoryRow
        {
            public int OrderId { get; init; }
            public string OldStatus { get; init; } = string.Empty;
            public string NewStatus { get; init; } = string.Empty;
            public string Reason { get; init; } = string.Empty;
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