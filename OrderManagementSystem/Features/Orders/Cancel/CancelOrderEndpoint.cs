using Asp.Versioning;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Orders.Cancel
{
    [ApiVersion("1.0")]
    [Microsoft.AspNetCore.Mvc.Route("api/v{version:apiVersion}/orders")]
    [Tags("Orders")]
    public class CancelOrderEndpoint:ApiControllerBase
    {
        private readonly CancelOrderHandler _handler;

        public CancelOrderEndpoint(CancelOrderHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler); 
            _handler = handler;
        }

        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(CancelOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CancelOrderResponse>> Cancel(
                    [FromRoute] int id,
                    CancellationToken cancellationToken)
        {
            var result = await _handler.HandleAsync(
                new CancelOrderRequest(id),
                cancellationToken);

            return HandleResult(result);
        }

    }

}
