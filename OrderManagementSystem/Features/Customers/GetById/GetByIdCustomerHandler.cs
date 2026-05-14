using Dapper;
using FluentValidation;
using OrderManagementSystem.Common.Results;
using OrderManagementSystem.Common.Validation;
using OrderManagementSystem.Infrastructure;
using System.Data;
using System.Data.Common;
using System.IO.Enumeration;
using System.Net;

namespace OrderManagementSystem.Features.Customers.GetById
{
    public class GetCustomerByIdHandler
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IValidator<GetCustomerByIdRequest> _validator;  

        public GetCustomerByIdHandler(IDbConnectionFactory dbConnectionFactory,IValidator<GetCustomerByIdRequest> validator)
        {
            ArgumentNullException.ThrowIfNull(dbConnectionFactory);
            ArgumentNullException.ThrowIfNull(validator); 
            _dbConnectionFactory = dbConnectionFactory;
            _validator = validator;
        }

        //ThreadPoolExcpetionFLow() A= > new{ 

        public async Task<Result<GetCustomerByIdResponse>> HandleAsync(GetCustomerByIdRequest request, CancellationToken cancellationToken )
        {
            ArgumentNullException.ThrowIfNull(request);

            var validationResult = await _validator.ValidateAsync(request,cancellationToken); 

            if(!validationResult.IsValid)
            {
                var errors = validationResult.ToErrorList();
                return Result.Failure<GetCustomerByIdResponse>(errors);
            }

            const string sql = """
                Select 
                CustomerId,
                FirstName,
                LastName,
                Email  from Customers  
                Where CustomerId=@CustomerId
                """;

            var parameters = new
            {   
                CustomerId = request.Id
            };

            using DbConnection connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);

           await  connection.OpenAsync(cancellationToken);

            var customer = await connection.QuerySingleOrDefaultAsync<GetCustomerByIdResponse>(
                 new CommandDefinition(
                     commandText:sql, 
                     parameters:parameters,
                     cancellationToken:cancellationToken
                     )
                ); 

            //ThreadPoolExcpetionFLow() a > new{

            if(customer is  null)
            {
                return Result.Failure<GetCustomerByIdResponse>(CustomerErrors.NotFound(request.Id));
            }

            return Result.Success(customer); 
        }
    }
}
