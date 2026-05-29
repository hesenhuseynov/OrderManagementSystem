using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Products.Search
{
    public static class SearchProductsEndpoint
    {
        public static RouteGroupBuilder MapSearchProductsEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("search", async (
                [FromQuery] string? query,
                SearchProductsHandler handler,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var request = new SearchProductsRequest(query ?? string.Empty);
                var result = await handler.HandleAsync(request, cancellationToken);

                return result.ToEndpointResult(httpContext);
            })
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithName("SearchProducts")
            .Produces<IReadOnlyList<SearchProductResponse>>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
