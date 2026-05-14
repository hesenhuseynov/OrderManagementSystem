using System.Data;

namespace OrderManagementSystem.Common.Outbox
{
    public interface IOutboxWriter
    {
        Task AddAsync<TPayload>(IDbConnection connection, IDbTransaction transaction, string eventType, string aggregateType,
            string aggregateId, TPayload payload, CancellationToken cancellationToken);
    }
}
