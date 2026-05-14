using Asp.Versioning;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Products.Search
{
    [ApiVersion("1.0")]
    [Microsoft.AspNetCore.Mvc.Route("api/v{version:apiVersion}/products")]
    [Tags("Products")]

    public class SearchProductsEndpoint:ApiControllerBase
    {
        private readonly SearchProductsHandler _handler;

        public SearchProductsEndpoint(SearchProductsHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            _handler = handler;
        }

        [HttpGet("search")]
        [ProducesResponseType(typeof(IReadOnlyList<SearchProductResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]

        public async Task<ActionResult<IReadOnlyList<SearchProductResponse>>>Search([FromQuery] string query, CancellationToken cancellationToken)
        {
            var request = new SearchProductsRequest(query);

            var result = await _handler.HandleAsync(request, cancellationToken);
           
            return HandleResult(result);
        }
    }
}
