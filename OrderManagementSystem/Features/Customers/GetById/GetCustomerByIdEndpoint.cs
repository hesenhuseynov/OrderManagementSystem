using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Customers.GetById
{
    public static class GetCustomerByIdEndpoint
    {
        public static RouteGroupBuilder MapGetCustomerByIdEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("{id:int}", async (
                [FromRoute] int id,
                GetCustomerByIdHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(new GetCustomerByIdRequest(id), cancellationToken);

                return result.ToEndpointResult(httpContext);
            })
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("GetCustomerById")
            .Produces<GetCustomerByIdResponse>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
