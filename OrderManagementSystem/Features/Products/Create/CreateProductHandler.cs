using Dapper;
using FluentValidation;
using Microsoft.Data.SqlClient;
using OrderManagementSystem.Common.Outbox;
using OrderManagementSystem.Common.Results;
using OrderManagementSystem.Common.Validation;
using OrderManagementSystem.Features.Products.Events;
using OrderManagementSystem.Infrastructure;
using System.Data;
using System.Data.Common;

namespace OrderManagementSystem.Features.Products.Create
{
    public sealed class CreateProductHandler
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IValidator<CreateProductRequest> _validator;
        private readonly IOutboxWriter _outboxWriter;
        private readonly ILogger<CreateProductHandler> _logger;

        public CreateProductHandler(
            IDbConnectionFactory dbConnectionFactory,
            IValidator<CreateProductRequest> validator,
            IOutboxWriter outboxWriter,
            ILogger<CreateProductHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(dbConnectionFactory);
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(outboxWriter);
            ArgumentNullException.ThrowIfNull(logger);

            _dbConnectionFactory = dbConnectionFactory;
            _validator = validator;
            _outboxWriter = outboxWriter;
            _logger = logger;
        }

        public async Task<Result<CreateProductResponse>> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToErrorList();
                return Result.Failure<CreateProductResponse>(errors);
            }


            CreateProductResponse? createdProduct = null;

            using (DbConnection connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken))
            {
                connection.Open();
                using var transaction = connection.BeginTransaction();

                try
                {
                    const string duplicateSkuSql = """

                        SELECT CAST (
                          CASE WHEN EXISTS (
                          SELECT 1  From dbo.Products Where  Sku=@Sku
                          )
                        THEN 1 ELSE 0  END  AS bit 
                        )
                        """;

                    var duplicateSku = await connection.QuerySingleAsync<bool>(
                        new CommandDefinition(duplicateSkuSql, new
                        {
                            request.Sku
                        }, transaction: transaction, cancellationToken: cancellationToken)
                        );

                    if (duplicateSku)
                    {
                       await transaction.RollbackAsync();

                        return Result.Failure<CreateProductResponse>(ProductErrors.DuplicateSku(request.Sku));
                    }

                    const string insertProductSql = """
                    INSERT INTO dbo.Products
                    (
                        Sku,
                        ProductName,
                        Price,
                        StockQuantity,
                        IsActive,
                        CreatedAt
                    )
                    OUTPUT
                        INSERTED.ProductId,
                        INSERTED.Sku,
                        INSERTED.ProductName,
                        INSERTED.Price,
                        INSERTED.StockQuantity,
                        INSERTED.IsActive
                    VALUES
                    (
                        @Sku,
                        @ProductName,
                        @Price,
                        @StockQuantity,
                        1,
                        SYSUTCDATETIME()
                    );
                    """;

                    createdProduct = await connection.QuerySingleAsync<CreateProductResponse>(new CommandDefinition(
                        insertProductSql, new
                        {
                            request.Sku,
                            request.ProductName,
                            request.Price,
                            request.StockQuantity
                        }, transaction: transaction, cancellationToken: cancellationToken
                        ));

                    var productCreatedEvent = new ProductCreatedEvent(
                        ProductId: createdProduct.ProductId,
                        Sku: createdProduct.Sku,
                        ProductName: createdProduct.ProductName,
                         Price: createdProduct.Price,
                        IsActive: createdProduct.IsActive
                        );

                    await _outboxWriter.AddAsync(
                                      connection,
                                      transaction,
                                      OutBoxEventTypes.ProductCreated,
                                      aggregateType: "Product",
                                      aggregateId: createdProduct.ProductId.ToString(),
                                      payload: productCreatedEvent,
                                      cancellationToken);

                   await transaction.CommitAsync();
                }

                catch(SqlException ex) when( ex.Number is  2601  or 2627)
                {
                   await transaction.RollbackAsync();
                    _logger.LogWarning(ex, "Duplicate product SKU detected while creating  product. Sku:{Sku}", request.Sku);
                    return Result.Failure<CreateProductResponse>(ProductErrors.DuplicateSku(request.Sku)); 
                }

                catch
                {
                   await transaction.RollbackAsync();
                    throw;
                }

            }

            if (createdProduct is null)
            {
                throw new InvalidOperationException(
                    "Create product flow reached an invalid internal state.");
            }

            _logger.LogInformation(
          "Product created successfully and ProductCreated event was written to outbox. ProductId: {ProductId}, Sku: {Sku}",
          createdProduct.ProductId,
          createdProduct.Sku);

            return Result.Success(createdProduct);

        }


    }

}


