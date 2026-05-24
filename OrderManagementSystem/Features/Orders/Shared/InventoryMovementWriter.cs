using Dapper;
using System.Data.Common;

namespace OrderManagementSystem.Features.Orders.Shared
{
    internal sealed record InventoryMovementInsertRow(
       int ProductId,
       int OrderId,
       string MovementType,
       int Quantity,
       string Reason);

    internal static class InventoryMovementWriter
    {
        public static async Task AddManyAsync(
            DbConnection connection,
            DbTransaction transaction,
            IEnumerable<InventoryMovementInsertRow> movements,
            CancellationToken cancellationToken)
        {
            const string sql = """
        INSERT INTO dbo.InventoryMovements
        (
            ProductId,
            OrderId,
            MovementType,
            Quantity,
            Reason
        )
        VALUES
        (
            @ProductId,
            @OrderId,
            @MovementType,
            @Quantity,
            @Reason
        );
        """;

            await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    movements,
                    transaction: transaction,
                    cancellationToken: cancellationToken));
        }
    }
}
