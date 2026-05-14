using Dapper;
using FluentValidation;
using OrderManagementSystem.Common.Models;
using OrderManagementSystem.Common.Results;
using OrderManagementSystem.Common.Validation;
using OrderManagementSystem.Infrastructure;
using System.Data.Common;

namespace OrderManagementSystem.Features.Customers.GetAllCustomer
{
    public sealed class GetAllCustomersHandler
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly IValidator<GetAllCustomerRequest> _validator;

        public GetAllCustomersHandler(
            IDbConnectionFactory connectionFactory,
            IValidator<GetAllCustomerRequest> validator)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);
            ArgumentNullException.ThrowIfNull(validator);

            _connectionFactory = connectionFactory;
            _validator = validator;
        }

        public async Task<Result<PagedResult<GetAllCustomersResponse>>> HandleAsync(
            GetAllCustomerRequest request,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return Result.Failure<PagedResult<GetAllCustomersResponse>>(
                    validationResult.ToErrorList());
            }

            const string sql = """
                SELECT COUNT(*)
                FROM dbo.Customers;

                SELECT
                    CustomerId,
                    FirstName,
                    LastName,
                    Email,
                    Phone,
                    CreatedAt
                FROM dbo.Customers
                ORDER BY CustomerId DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;
                """;

            var parameters = new
            {
                Offset = (request.PageNumber - 1) * request.PageSize,
                request.PageSize
            };

            using DbConnection connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            await connection.OpenAsync(cancellationToken);

            using var multi = await connection.QueryMultipleAsync(
                new CommandDefinition(
                    sql,
                    parameters,
                    cancellationToken: cancellationToken));

            var totalCount = await multi.ReadSingleAsync<int>();

            var items = (await multi.ReadAsync<GetAllCustomersResponse>()).AsList();

            var pagedResult = new PagedResult<GetAllCustomersResponse> 
            {
                Items = items,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };

            return Result.Success(pagedResult);
        }
    }
}