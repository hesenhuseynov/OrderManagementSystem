using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Products.Create
{
    public static class CreateProductEndpoint
    {
        public static RouteGroupBuilder MapCreateProductEndpoint(this RouteGroupBuilder group)
        {
            group.MapPost("", async (
                [FromBody] CreateProductRequest request,
                CreateProductHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request, cancellationToken);

                return result.ToCreatedEndpointResult(httpContext);
            })
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("CreateProduct")
            .Produces<CreateProductResponse>(StatusCodes.Status201Created)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
