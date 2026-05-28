using FluentAssertions;
using Microsoft.AspNetCore.Http.Json;
using OrderManagementSystem.Common.Outbox;
using OrderManagementSystem.Features.Orders.Pay.Contracts;
using OrderManagementSystem.Infrastructure.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace OrderManagementSystem.UnitTests.Common.Outbox
{
    public sealed   class OutboxEventProcessorTests
    {
        private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

        [Fact] 
        public async Task  TaskProcessAsync_Should_Process_PaymentCompleted_Event_Without_Throwing()
        {
            var processor = new OutboxEventProcessor(new FakeProductSearchIndexer());

            var paymentCompletedEvent = new PaymentCompletedEvent(
                OrderId: 10,
                PaymentId: 25,
                TransactionId: "txn_123",
                Amount: 150m,
                Currency: "AZN",
                PaidAt: DateTime.UtcNow
                );

            var message = CreateOutboxMessage(eventType: OutBoxEventTypes.PaymentCompleted,
                 payload: JsonSerializer.Serialize(paymentCompletedEvent, jsonOptions));

            var act =  async() =>  await processor.ProcessAsync(message, CancellationToken.None);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task ProcessAsync_Should_Throw_When_PaymentCompleted_Payload_Is_Invalid()
        {
            var processor = new OutboxEventProcessor(new FakeProductSearchIndexer());

            var invalidPaymentCompletedEvent = new PaymentCompletedEvent(
                OrderId: 0,
                PaymentId: 25,
                TransactionId: "txn_123",
                Amount: 150m,
                Currency: "AZN",
                PaidAt: DateTime.UtcNow);

            var message = CreateOutboxMessage(
                eventType: OutBoxEventTypes.PaymentCompleted,
                payload: JsonSerializer.Serialize(invalidPaymentCompletedEvent, jsonOptions));

            var act = async () => await processor.ProcessAsync(
                message,
                CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Invalid PaymentCompleted event payload*");
        }

        [Fact]  
        public async Task ProcessAsync_Should_Throw_When_EventType_Is_Unsupported()
        {
            var processor = new OutboxEventProcessor(new FakeProductSearchIndexer());

            var message = CreateOutboxMessage(
                eventType: "UnknownEvent",
                payload: "{}");

            var act = async () => await processor.ProcessAsync(
                message,
                CancellationToken.None);

            await act.Should().ThrowAsync<NotSupportedException>()
                .WithMessage("*Unsupported outbox event type:*");
        }

        private static OutboxMessage CreateOutboxMessage(
        string eventType,
        string payload)
        {
            return new OutboxMessage
            {
                OutboxEventId = 1,
                EventId = Guid.NewGuid(),
                EventType = eventType,
                AggregateType = "Order",
                AggregateId = "10",
                Payload = payload,
                RetryCount = 0
            };
        }

        private sealed class FakeProductSearchIndexer : IProductSearchIndexer
        {
            public Task IndexAsync(ProductSearchDocument document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
