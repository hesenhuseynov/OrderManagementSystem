using System.Data;
using System.Data.Common;

namespace OrderManagementSystem.Infrastructure
{
    public interface IDbConnectionFactory
    {
        Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken);

    }
}
