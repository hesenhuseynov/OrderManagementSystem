using Dapper;
using OrderManagementSystem.Infrastructure;
using System.Data;

namespace OrderManagementSystem.Common.Outbox
{
    public sealed class SqlOutBoxStore : IOutboxStore
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        public SqlOutBoxStore(IDbConnectionFactory dbConnectionFactory)
        {
            ArgumentNullException.ThrowIfNull(dbConnectionFactory);

            _dbConnectionFactory = dbConnectionFactory;
        }

        public async Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
            Guid lockId,
            int batchSize,
            int lockTimeoutSeconds,
            int maxRetryCount,
            CancellationToken cancellationToken)
        {
            const string sql = """
                ;WITH NextEvents AS
                (
                    SELECT TOP (@BatchSize)
                        OutboxEventId,
                        EventId,
                        EventType,
                        AggregateType,
                        AggregateId,
                        Payload,
                        OccurredAt,
                        RetryCount,
                        LockedAt,
                        LockId
                    FROM dbo.OutboxEvents WITH (READPAST, UPDLOCK, ROWLOCK, READCOMMITTEDLOCK)
                    WHERE ProcessedAt IS NULL
                      AND RetryCount < @MaxRetryCount
                      AND
                      (
                          LockedAt IS NULL
                          OR LockedAt < DATEADD(SECOND, -@LockTimeoutSeconds, SYSUTCDATETIME())
                      )
                    ORDER BY OccurredAt ASC, OutboxEventId ASC
                )
                UPDATE NextEvents
                SET
                    LockedAt = SYSUTCDATETIME(),
                    LockId = @LockId
                OUTPUT
                    INSERTED.OutboxEventId,
                    INSERTED.EventId,
                    INSERTED.EventType,
                    INSERTED.AggregateType,
                    INSERTED.AggregateId,
                    INSERTED.Payload,
                    INSERTED.OccurredAt,
                    INSERTED.RetryCount,
                    INSERTED.LockId;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            await connection.OpenAsync(cancellationToken);

            using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            try
            {
                var messages = await connection.QueryAsync<OutboxMessage>(
                    new CommandDefinition(
                        sql,
                        new
                        {
                            LockId = lockId,
                            BatchSize = batchSize,
                            LockTimeoutSeconds = lockTimeoutSeconds,
                            MaxRetryCount = maxRetryCount
                        },
                        transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                return messages.ToList();
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        public async Task MarkProcessedAsync(
            long outboxEventId,
            Guid lockId,
            CancellationToken cancellationToken)
        {
            const string sql = """
                UPDATE dbo.OutboxEvents
                SET
                    ProcessedAt = SYSUTCDATETIME(),
                    LockedAt = NULL,
                    LockId = NULL,
                    LastError = NULL
                WHERE OutboxEventId = @OutboxEventId
                  AND LockId = @LockId
                  AND ProcessedAt IS NULL;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            await connection.OpenAsync(cancellationToken);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        OutboxEventId = outboxEventId,
                        LockId = lockId
                    },
                    cancellationToken: cancellationToken));
        }

        public async Task MarkFailedAsync(
            long outboxEventId,
            Guid lockId,
            string error,
            CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(error);

            const string sql = """
                UPDATE dbo.OutboxEvents
                SET
                    RetryCount = RetryCount + 1,
                    LastError = @LastError,
                    LockedAt = NULL,
                    LockId = NULL
                WHERE OutboxEventId = @OutboxEventId
                  AND LockId = @LockId
                  AND ProcessedAt IS NULL;
                """;

            using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
            await connection.OpenAsync(cancellationToken);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        OutboxEventId = outboxEventId,
                        LockId = lockId,
                        LastError = error
                    },
                    cancellationToken: cancellationToken));
        }
    }
}