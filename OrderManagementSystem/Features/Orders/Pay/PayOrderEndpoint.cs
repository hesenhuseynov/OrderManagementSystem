using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Orders.Pay
{
    public static class PayOrderEndpoint
    {
        public static RouteGroupBuilder MapPayOrderEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("{orderId:int}/pay", async (
                [FromRoute] int orderId,
                [FromBody] PayOrderRequest request,
                PayOrderHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(orderId, request, cancellationToken);

                return result.ToEndpointResult(httpContext);
            })
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("PayOrder")
            .Produces<PayOrderResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
