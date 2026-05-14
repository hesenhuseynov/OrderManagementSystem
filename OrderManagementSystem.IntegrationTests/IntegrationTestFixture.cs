using Dapper;
using Microsoft.Data.SqlClient;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace OrderManagementSystem.IntegrationTests
{
    public sealed class IntegrationTestFixture : IAsyncLifetime
    {
            private readonly MsSqlContainer _sqlContainer;
            private readonly RedisContainer _redisContainer;

            private string _sqlConnectionString = string.Empty;
            private string _redisConnectionString = string.Empty;

            public CustomWebApplicationFactory Factory { get; private set; } = default!;
            public HttpClient Client { get; private set; } = default!;

            public IntegrationTestFixture()
            {
                _sqlContainer = new MsSqlBuilder()
                    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                    .WithPassword("yourStrong(!)Password123")
                    .Build();

                _redisContainer = new RedisBuilder()
                    .WithImage("redis:7-alpine")
                    .Build();
            }

            public async Task InitializeAsync()
            {
                await _sqlContainer.StartAsync();
                await _redisContainer.StartAsync();

                _sqlConnectionString = _sqlContainer.GetConnectionString();

                _redisConnectionString =
                    $"{_redisContainer.GetConnectionString()},defaultDatabase=1,abortConnect=false,allowAdmin=true";

                await ApplyDatabaseSchemaAsync();

                Factory = new CustomWebApplicationFactory(
                    _sqlConnectionString,
                    _redisConnectionString);

                Client = Factory.CreateClient();
            }

            public async Task DisposeAsync()
            {
                Client.Dispose();

                await Factory.DisposeAsync();

                await _redisContainer.DisposeAsync();
                await _sqlContainer.DisposeAsync();
            }

            public IDbConnection CreateConnection()
                => new SqlConnection(_sqlConnectionString);

            public async Task ResetDatabaseAsync()
            {
            const string sql = """
                DELETE FROM dbo.OutboxEvents 
                DELETE FROM dbo.InventoryMovements;
                DELETE FROM dbo.Payments;
                DELETE FROM dbo.OrderStatusHistory;
                DELETE FROM dbo.OrderItems;
                DELETE FROM dbo.Orders;
                DELETE FROM dbo.Products;
                DELETE FROM dbo.Customers;
                """;
                using var connection = CreateConnection();
                connection.Open();

                await connection.ExecuteAsync(sql);
            }

            public async Task FlushRedisAsync()
            {
                await using var muxer = await ConnectionMultiplexer.ConnectAsync(_redisConnectionString);

                var endpoint = muxer.GetEndPoints().First();
                var server = muxer.GetServer(endpoint);

                await server.FlushDatabaseAsync(1);
            }

            public async Task<int> InsertCustomerAsync(
                string firstName = "Ali",
                string lastName = "Veli")
            {
                const string sql = """
                INSERT INTO dbo.Customers (FirstName, LastName, Email, IsActive)
                OUTPUT INSERTED.CustomerId
                VALUES (@FirstName, @LastName, @Email, 1);
                """;

                using var connection = CreateConnection();
                connection.Open();

                return await connection.QuerySingleAsync<int>(sql, new
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = $"{Guid.NewGuid()}@test.com"
                });
            }

            public async Task<int> InsertProductAsync(
                string productName = "Test Product",
                decimal price = 200m,
                int stockQuantity = 5)
            {
                const string sql = """
                INSERT INTO dbo.Products 
                (Sku, ProductName, Price, StockQuantity, IsActive, CreatedAt)
                OUTPUT INSERTED.ProductId
                VALUES (@Sku, @ProductName, @Price, @StockQuantity, 1, @CreatedAt);
                """;

                using var connection = CreateConnection();
                connection.Open();

                return await connection.QuerySingleAsync<int>(sql, new
                {
                    Sku = $"SKU-{Guid.NewGuid()}",
                    ProductName = productName,
                    Price = price,
                    StockQuantity = stockQuantity,
                    CreatedAt = DateTime.UtcNow
                });
            }

            private async Task ApplyDatabaseSchemaAsync()
            {
                var schemaPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "Database",
                    "schema.sql");

                if (!File.Exists(schemaPath))
                {
                    throw new FileNotFoundException(
                        $"Schema file was not found. Expected path: {schemaPath}");
                }

                var script = await File.ReadAllTextAsync(schemaPath);

                var batches = Regex.Split(
                    script,
                    @"^\s*GO\s*$",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);

                using var connection = new SqlConnection(_sqlConnectionString);
                await connection.OpenAsync();

                foreach (var batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch))
                    {
                        continue;
                    }

                    await connection.ExecuteAsync(batch);
                }
            }

        }


    }

