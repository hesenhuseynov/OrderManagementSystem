namespace OrderManagementSystem.Common.Outbox
{
    public class OutboxMessage
    {
        public long OutboxEventId { get; init; }

        public Guid EventId { get; init; }

        public string EventType { get; init; } = string.Empty;

        public string AggregateType { get; init; } = string.Empty;

        public string AggregateId { get; init; } = string.Empty;

        public string Payload { get; init; } = string.Empty;

        public DateTime OccurredAt { get; init; }

        public int RetryCount { get; init; }

        public Guid? LockId { get; init; }
    }
}
