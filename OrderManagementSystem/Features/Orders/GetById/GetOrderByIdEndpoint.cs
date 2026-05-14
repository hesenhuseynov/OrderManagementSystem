using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;
namespace OrderManagementSystem.Features.Orders.GetById
{
    [ApiVersion( "1.0")]
    [Route("api/v{version:apiVersion}/orders")]
    [Tags("Orders")]
    public sealed class GetOrderByIdEndpoint :ApiControllerBase
    {
        private readonly GetOrderByIdHandler _handler;

        public GetOrderByIdEndpoint(GetOrderByIdHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler); 
            _handler = handler; 
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(GetOrderByIdResponse ) , StatusCodes.Status200OK)]
        [ProducesResponseType(typeof( ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status500InternalServerError)]
        
        public async Task<ActionResult<GetOrderByIdResponse>> GetById([FromRoute]  int id, CancellationToken cancellationToken)
        {
            var result = await _handler.HandleAsync(new GetOrderByIdRequest(id), cancellationToken);

            return HandleResult(result);
        }

    }
}
