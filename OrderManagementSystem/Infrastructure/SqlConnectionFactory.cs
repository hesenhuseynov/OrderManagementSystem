using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Infrastructure.Configuration;
using System.Data;
using System.Data.Common;
using System.Security.AccessControl;

namespace OrderManagementSystem.Infrastructure
{
    public sealed  class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        public SqlConnectionFactory(IOptions<DatabaseOptions> options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var connectionString = options.Value.DefaultConnection;
             
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Database Connection string is  not configured . Check ConnectioNStrings: DefaultCOnneciton ");
            }
            _connectionString = connectionString;
        }
       
        public Task<DbConnection> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<DbConnection>(new SqlConnection(_connectionString));
        }
    }
}