using Dapper;
using FluentValidation;
using Microsoft.Extensions.Options;
using OrderManagementSystem.Common.Caching;
using OrderManagementSystem.Common.Results;
using OrderManagementSystem.Common.Validation;
using OrderManagementSystem.Features.Customers;
using OrderManagementSystem.Features.Orders.Create;
using OrderManagementSystem.Infrastructure;
using System.Data;
using System.Data.Common;

namespace OrderManagementSystem.Features.Orders.GetById
{
    public class GetOrderByIdHandler
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IValidator<GetOrderByIdRequest> _validator ;
        private readonly IResilientCacheService _resilientCacheService;
        private readonly ILogger<GetOrderByIdHandler> _logger;
        private readonly CacheSettings _cacheSettings; 

        public GetOrderByIdHandler(IDbConnectionFactory connectionFactory,
            IValidator<GetOrderByIdRequest> validator,IResilientCacheService resilientCacheService , ILogger< GetOrderByIdHandler> logger,IOptions<CacheSettings> 
             cacheOptions)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);
            ArgumentNullException.ThrowIfNull(validator);

            _validator = validator;
            _connectionFactory = connectionFactory;
            _resilientCacheService = resilientCacheService;
            _logger=logger;
            _cacheSettings = cacheOptions.Value;
        }

        public async Task<Result<GetOrderByIdResponse>>HandleAsync(GetOrderByIdRequest request,CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult =  await  _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for GetOrderById.OrderId: {OrderId} ", request.Id); 
                var errors = validationResult.ToErrorList();
              return   Result.Failure<GetOrderByIdResponse>(errors); 
            }


            var cacheKey = CacheKeys.OrderById(request.Id);

            var cacheResult = await _resilientCacheService.TryGetAsync<GetOrderByIdResponse>(
                cacheKey,
                cancellationToken
                ); 

            if(cacheResult.IsHit)
            {
                _logger.LogInformation(
                       "Order detail returned  from cache OrderId: {OrderId} ,CacheKey:{CacheKey}",
                       request.Id, cacheKey
                     );

                return Result.Success(cacheResult.Value!);
            }


            _logger.LogInformation(
                "Order detail cache miss or cache unavailable. Loading from database. OrderId: {OrderId}, CacheKey: {CacheKey}",
                request.Id,
                cacheKey);


            using DbConnection connection = await  _connectionFactory.CreateConnectionAsync(cancellationToken);
            
            await connection.OpenAsync();

            const string sql = """
                SELECT  
                 o.OrderId,
                 o.OrderNumber, 
                 o.CustomerId,
                  CONCAT(c.FirstName, ' ', c.LastName) AS CustomerFullName, 
                 o.OrderDate,
                 o.Status,
                 o.Currency, 
                 o.TotalAmount
                FROM dbo.Orders  o INNER JOIN  dbo.Customers c 
                ON c.CustomerId=o.CustomerId  
                Where o.OrderId=@OrderId;

                              SELECT
                    oi.ProductId,
                    p.ProductName,
                    oi.Quantity,
                    oi.UnitPrice,
                    oi.LineTotal 
                FROM dbo.OrderItems oi
                INNER JOIN dbo.Products p ON p.ProductId = oi.ProductId
                WHERE oi.OrderId = @OrderId
                ORDER BY oi.OrderItemId;
                """;

            using var multi = await connection.QueryMultipleAsync(new CommandDefinition(

                  sql, new { OrderId = request.Id },
                  cancellationToken: cancellationToken
                ));

            var order = await multi.ReadFirstOrDefaultAsync<GetOrderByIdResponse>();
             
             if(order is null)
            {
                _logger.LogInformation("Order not found  in database  . OrderId:{OrderId}", request.Id);   

                return Result.Failure<GetOrderByIdResponse>(OrderErrors.OrderNotFound(request.Id)); 
            }

            var items = (await multi.ReadAsync<GetOrderByIdItemResponse>()).ToList();


            if (items.Count == 0)
            {
                _logger.LogWarning("Order found without items. Order:{OrderId} ", request.Id);
            }


            var response = order with
            {
                Items = items
            };

            var ttl = TimeSpan.FromMinutes(_cacheSettings.OrderByIdTtlMinutes);

            await _resilientCacheService.TrySetAsync(
                          cacheKey,
                          response,
                         ttl,
                          cancellationToken);
            _logger.LogInformation(
    "Order detail loaded from database. Cache write was attempted. CacheKey: {CacheKey}, TTL: {TtlMinutes} minutes",
    cacheKey,
    _cacheSettings.OrderByIdTtlMinutes);

            return  Result.Success(response); 

        }
    }
}
