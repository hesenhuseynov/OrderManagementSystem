using Asp.Versioning;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Orders.Pay
{
    [ApiVersion("1.0")]
    [Microsoft.AspNetCore.Mvc.Route("api/v{version:apiVersion}/orders")]
    [Tags("Orders")]
    public sealed class PayOrderEndpoint:ApiControllerBase
    {
        private readonly PayOrderHandler _handler;

        public PayOrderEndpoint(PayOrderHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            _handler = handler;  
        }

        [HttpPost("{orderId:int}/pay")]
        [ProducesResponseType(typeof(PayOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

        public async Task<ActionResult<PayOrderResponse>> Pay([FromRoute]  int orderId, [FromBody] PayOrderRequest  request ,CancellationToken cancellationToken)
        {
            var result = await _handler.HandleAsync(orderId, request, cancellationToken);
            return HandleResult(result);
        }
    }
}
