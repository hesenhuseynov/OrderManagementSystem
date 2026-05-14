using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;

namespace OrderManagementSystem.Features.Products.Create
{

    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/products")]
    [Tags("Products")]
    public class CreateProductEndpoint:ApiControllerBase
    {
        private readonly CreateProductHandler _handler;  

        public CreateProductEndpoint(CreateProductHandler   handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            _handler = handler; 
        }

        [HttpPost]
        [ProducesResponseType(typeof(CreateProductResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateProductResponse>> Create(
         [FromBody] CreateProductRequest request,
         CancellationToken cancellationToken)
        {
            var result = await _handler.HandleAsync(request, cancellationToken);

            return HandleCreatedResult(result);
        }
    }
}
