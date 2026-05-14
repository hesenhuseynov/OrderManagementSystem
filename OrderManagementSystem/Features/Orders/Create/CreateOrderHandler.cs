using Dapper;
using FluentValidation;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Common.Caching;
using OrderManagementSystem.Common.Results;
using OrderManagementSystem.Common.Validation;
using OrderManagementSystem.Features.Orders.GetById;
using OrderManagementSystem.Infrastructure;
using System.Data;
using System.Drawing;

namespace OrderManagementSystem.Features.Orders.Create
{
    public sealed class CreateOrderHandler
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IValidator<CreateOrderRequest> _validator;
        private readonly IResilientCacheService _resilientCacheService;
        private readonly CacheSettings _cacheSettings;
        private readonly ILogger<CreateOrderHandler> _logger; 

        public CreateOrderHandler(IDbConnectionFactory dbConnectionFactory,
            IValidator<CreateOrderRequest> validator, IResilientCacheService resilientCacheService,
            IOptions<CacheSettings> cacheOptions,ILogger<CreateOrderHandler> logger)
        {
            ArgumentNullException.ThrowIfNull(dbConnectionFactory);
            ArgumentNullException.ThrowIfNull(validator);
            ArgumentNullException.ThrowIfNull(resilientCacheService);
            ArgumentNullException.ThrowIfNull(cacheOptions);
            ArgumentNullException.ThrowIfNull(logger);

            _dbConnectionFactory = dbConnectionFactory;
            _validator = validator;
            _resilientCacheService = resilientCacheService;
            _cacheSettings = cacheOptions.Value;
            _logger = logger;
        }

        public async Task<Result<CreateOrderResponse>> HandleAsync(CreateOrderRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToErrorList();

                return Result.Failure<CreateOrderResponse>(errors);
            }

            var hasDuplicateProducts = request.Items 
                .GroupBy(x => x.ProductId)
                .Any(g => g.Count() > 1);

            if (hasDuplicateProducts)
            {
                return Result.Failure<CreateOrderResponse>(OrderErrors.DuplicateProducts);
            }

            CreateOrderResponse? createdOrder = null;
            GetOrderByIdResponse? cacheResponse = null;

            using (var connection =  await _dbConnectionFactory.CreateConnectionAsync(cancellationToken))
            {
               await  connection.OpenAsync(cancellationToken);

                using var transaction = await  connection.BeginTransactionAsync(cancellationToken);

                try
                {
                    const string customerSql = """
                    
                    SELECT  FirstName, LastName 
                    FROM dbo.Customers 
                    Where CustomerId  =@CustomerId 
                    AND IsActive=1
                    """;

                    var customer = await connection.QueryFirstOrDefaultAsync<CustomerSnapshot>(
                        new CommandDefinition(
                           customerSql, new { request.CustomerId }, transaction: transaction, cancellationToken: cancellationToken
                            ));

                    if (customer is null)
                    {
                        return Result.Failure<CreateOrderResponse>(OrderErrors.CustomerNotFound(request.CustomerId));
                    }

                    var productIds = request.Items.Select(x => x.ProductId).ToArray();

                    const string productSql = """
                     
                    SELECT ProductId,ProductName,Price,StockQuantity From dbo.Products 
                         WITH (UPDLOCK,HoldLock) 
                        Where ProductId  IN @ProductIds   
                        AND  IsActive=1 
                    """;


                    var products = (await connection.QueryAsync<ProductSnapshot>(
                         new CommandDefinition(
                               productSql,
                             new { ProductIds = productIds },
                             transaction: transaction,
                             cancellationToken: cancellationToken
                             )
                        )).ToList();

                    if (products.Count != productIds.Length)
                    {
                        var foundIds = products.Select(x => x.ProductId).ToHashSet();
                        var missingProductId = productIds.First(id => !foundIds.Contains(id));
                        return Result.Failure<CreateOrderResponse>(
                            OrderErrors.ProductNotFound(missingProductId)
                            );
                    }

                    var productMap = products.ToDictionary(x => x.ProductId);

                    foreach (var item in request.Items)
                    {
                        var product = productMap[item.ProductId];

                        if (product.StockQuantity < item.Quantity)
                        {
                            return Result.Failure<CreateOrderResponse>(OrderErrors.InsufficientStock(item.ProductId));
                        }
                    }

                    decimal TotalAmount = request.Items.Sum(item =>
                    {
                        var product = productMap[item.ProductId];
                        return product.Price * item.Quantity;
                    });


                    const string insertOrderSql = """

                     INSERT INTO dbo.Orders(
                      
                       CustomerId,
                       Status,
                       Currency, 
                       TotalAmount 
                     )
                      OUTPUT 
                      INSERTED.OrderId,
                      INSERTED.OrderNumber,
                      INSERTED.CustomerId, 
                      INSERTED.OrderDate, 
                      INSERTED.Status,
                      INSERTED.Currency, 
                      INSERTED.TotalAmount

                      VALUES(
                      @CustomerId, 
                      @Status, 
                      @Currency,
                      @TotalAmount
                      );  

                    """;

                    createdOrder = await connection.QuerySingleAsync<CreateOrderResponse>(

                         new CommandDefinition(
                             insertOrderSql,
                             new
                             {
                                 CustomerId = request.CustomerId,
                                 Status = OrderConstants.Statuses.Pending,
                                 Currency = OrderConstants.Currency.Azn,
                                 TotalAmount = TotalAmount
                             },
                             transaction: transaction,
                             cancellationToken: cancellationToken
                            )

                       );

                    const string insertOrderItemsSql = """
                      
                    INSERT INTO  dbo.OrderItems(
                      OrderId,
                      ProductId,
                      Quantity,
                      UnitPrice
                    )
                    VALUES( 
                     @OrderId, 
                     @ProductId,
                     @Quantity, 
                     @UnitPrice
                    )
                    """;

                    var orderItemParameters = request.Items.Select(item => new
                    {
                        OrderId = createdOrder.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = productMap[item.ProductId].Price
                    });


                 await connection.ExecuteAsync(
                           new CommandDefinition(
                               insertOrderItemsSql,
                               orderItemParameters,
                               transaction: transaction,
                                cancellationToken: cancellationToken
                               )
                          );


                    const string updatestockSql = """
                     UPDATE  dbo.Products  SET StockQuantity= StockQuantity- @Quantity 
                     WHERE  ProductId =  @ProductId  

                    """;

                    var stockParameters = request.Items.Select(item => new
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    });

                  var  updatedRows = await connection.ExecuteAsync(new CommandDefinition(updatestockSql, stockParameters, transaction: transaction, cancellationToken: cancellationToken));

                    if (updatedRows != request.Items.Count)
                    {
                        throw new InvalidOperationException("Stock  update  afected unexpected number of rows");
                    }


                    const string insertInventoryMovementsSql = """
                     INSERT  INTO dbo.InventoryMovements(
                         ProductId, 
                         OrderId,
                         MovementType,
                         Quantity, 
                         Reason
                     )
                      VALUES(
                       @ProductId,
                       @OrderId, 
                       @MovementType , 
                       @Quantity, 
                       @Reason
                      ); 
                    """;

                    var movementParameters = request.Items.Select(item => new
                    {
                        ProductId = item.ProductId,
                        OrderId = createdOrder.OrderId,
                        MovementType = OrderConstants.InventoryMovementTypes.Reserve,
                        Quantity = item.Quantity,
                        Reason = OrderConstants.Reasons.OrderCreated
                    });

                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            insertInventoryMovementsSql,
                            movementParameters,
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                    const string insertOrderStatusHistorySql = """
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
                            insertOrderStatusHistorySql,
                            new
                            {
                                OrderId = createdOrder.OrderId,
                                OldStatus = (string?)null,
                                NewStatus = OrderConstants.Statuses.Pending,
                                ChangedBy = OrderConstants.ChangedBy.System,
                                Reason = OrderConstants.Reasons.OrderCreated
                            },
                            transaction: transaction,
                            cancellationToken: cancellationToken));

                     cacheResponse = BuilderOrderCacheResponse(
                        createdOrder,
                        customer,
                        request,
                        productMap
                        );

                   await  transaction.CommitAsync();

                }
                catch
                {
                   await  transaction.RollbackAsync();
                    throw;
                }
            }

             if (createdOrder  is null   || cacheResponse is null)
            {
                throw new InvalidOperationException(
                      "Create order flow reached an invalid internal state.");
            }

                await _resilientCacheService.TrySetAsync(
                         CacheKeys.OrderById(createdOrder.OrderId),
                          cacheResponse,
                            TimeSpan.FromMinutes(_cacheSettings.OrderByIdTtlMinutes),
                             cancellationToken);


            _logger.LogInformation(
                  "Order created successfully. Cache warming was attempted. OrderId: {OrderId}, CacheKey: {CacheKey}",
                  createdOrder.OrderId,
                      CacheKeys.OrderById(createdOrder.OrderId));

            return Result.Success(createdOrder);

        }


        private  static  GetOrderByIdResponse BuilderOrderCacheResponse(CreateOrderResponse  createOrder 
            ,CustomerSnapshot customer,CreateOrderRequest request,  IReadOnlyDictionary<int ,ProductSnapshot> productMap)
        {
            return new GetOrderByIdResponse
            {
                OrderId = createOrder.OrderId,
                OrderNumber = createOrder.OrderNumber,
                CustomerId = createOrder.CustomerId,
                CustomerFullName = $"{customer.FirstName} {customer.LastName}".Trim(),
                OrderDate = createOrder.OrderDate,
                Status = createOrder.Status,
                Currency = createOrder.Currency,
                TotalAmount = createOrder.TotalAmount,
                Items = request.Items.Select(item => new GetOrderByIdItemResponse
                {
                    ProductId = item.ProductId,
                    ProductName = productMap[item.ProductId].ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = productMap[item.ProductId].Price,
                    LineTotal = productMap[item.ProductId].Price * item.Quantity
                }).ToList()
            }; 
        }

        private sealed class ProductSnapshot
        {
            public int ProductId { get; init; }
            public string ProductName { get; init; } = string.Empty;

            public decimal Price { get; init; }

            public int StockQuantity { get; init; }
        }

        private sealed class CustomerSnapshot
        {
            public string FirstName { get; init; } = string.Empty;
            public string LastName { get; init; } = string.Empty; 
        }
    }
}
