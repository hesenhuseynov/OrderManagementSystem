namespace OrderManagementSystem.Common.Outbox
{
    public interface IOutboxStore
    {
        Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(Guid lockId, int batchSize, int lockTimeoutSecond,
            int maxRetryCount, CancellationToken cancellationToken);

        Task MarkProcessedAsync(long outboxEventId, Guid lockId, CancellationToken cancellationToken);

        Task MarkFailedAsync(long outboxEventId, Guid lockId, string error, CancellationToken cancellationToken);
    }
}
