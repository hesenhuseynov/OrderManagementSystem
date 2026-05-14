using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Customers.GetById
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/customers")]
    [Tags("Customers")]

    public class GetCustomerByIdEndpoint:ApiControllerBase
    {
        private readonly GetCustomerByIdHandler _handler;

        
        public GetCustomerByIdEndpoint(GetCustomerByIdHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            _handler = handler; 
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(GetCustomerByIdResponse),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetCustomerByIdResponse>>GetCustomerById([FromRoute] int id,
            CancellationToken cancellationToken)
        {
            var result = await _handler.HandleAsync(new GetCustomerByIdRequest(id), cancellationToken);

            return HandleResult(result);
        }
    }
}
