using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Orders.Cancel
{
    public static class CancelOrderEndpoint
    {
        public static RouteGroupBuilder MapCancelOrderEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("{id:int}/cancel", async (
                [FromRoute] int id,
                CancelOrderHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new CancelOrderRequest(id),
                    cancellationToken);

                return result.ToEndpointResult(httpContext);
            })
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("CancelOrder")
            .Produces<CancelOrderResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
