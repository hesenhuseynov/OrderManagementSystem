using Dapper;
using System.Data;
using System.Text.Json;

namespace OrderManagementSystem.Common.Outbox
{
    public sealed class OutboxWriter : IOutboxWriter
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task AddAsync<TPayload>(
            IDbConnection connection,
            IDbTransaction transaction,
            string eventType,
            string aggregateType,
            string aggregateId,
            TPayload payload,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(transaction);
            ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
            ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);
            ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
            ArgumentNullException.ThrowIfNull(payload);

            const string sql = """
                INSERT INTO dbo.OutboxEvents
                (
                    EventId,
                    EventType,
                    AggregateType,
                    AggregateId,
                    Payload,
                    OccurredAt,
                    RetryCount,
                    CreatedAt
                )
                VALUES
                (
                    @EventId,
                    @EventType,
                    @AggregateType,
                    @AggregateId,
                    @Payload,
                    SYSUTCDATETIME(),
                    0,
                    SYSUTCDATETIME()
                );
                """;

            var jsonPayload = JsonSerializer.Serialize(payload, JsonOptions);

            var command = new CommandDefinition(
                sql,
                new
                {
                    EventId = Guid.NewGuid(),
                    EventType = eventType,
                    AggregateType = aggregateType,
                    AggregateId = aggregateId,
                    Payload = jsonPayload
                },
                transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }
    }
}
