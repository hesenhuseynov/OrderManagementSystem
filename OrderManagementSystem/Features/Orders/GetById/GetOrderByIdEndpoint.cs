using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Orders.GetById
{
    public static class GetOrderByIdEndpoint
    {
        public static RouteGroupBuilder MapGetOrderByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("{id:int}", async (
                [FromRoute] int id,
                GetOrderByIdHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetOrderByIdRequest(id), cancellationToken);

                return result.ToEndpointResult(httpContext);
            })
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("GetOrderById")
            .Produces<GetOrderByIdResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;

        }
    }
}
