namespace OrderManagementSystem.Common.Outbox
{
    public interface IOutboxEventProcessor
    {
        Task ProcessAsync(OutboxMessage message, CancellationToken cancellationToken);
    }
}
