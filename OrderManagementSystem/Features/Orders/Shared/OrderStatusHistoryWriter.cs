using Dapper;
using System.Data.Common;

namespace OrderManagementSystem.Features.Orders.Shared
{


    internal static class OrderStatusHistoryWriter
    {
        public static async Task AddAsync(
            DbConnection connection,
            DbTransaction transaction,
            int orderId,
            string? oldStatus,
            string newStatus,
            string changedBy,
            string reason,
            CancellationToken cancellationToken)
        {
            const string sql = """
        INSERT INTO dbo.OrderStatusHistory
        (
            OrderId,
            OldStatus,
            NewStatus,
            ChangedBy,
            Reason
        )
        VALUES
        (
            @OrderId,
            @OldStatus,
            @NewStatus,
            @ChangedBy,
            @Reason
        );
        """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        OrderId = orderId,
                        OldStatus = oldStatus,
                        NewStatus = newStatus,
                        ChangedBy = changedBy,
                        Reason = reason
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }
    }

}