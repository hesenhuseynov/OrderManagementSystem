using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Orders.Create
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/orders")]
    [Tags("Orders")]
    public sealed class CreateOrderEndpoint:ApiControllerBase
    {
        private readonly CreateOrderHandler _handler;
        public CreateOrderEndpoint(CreateOrderHandler handler)
        {
            _handler = handler;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateOrderResponse>> Create(
         [FromBody] CreateOrderRequest request,
         CancellationToken cancellationToken)
        {
            var result = await _handler.HandleAsync(request, cancellationToken);

            return HandleCreatedResult(result);
        }
    }
}
