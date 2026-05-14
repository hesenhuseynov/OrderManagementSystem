using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;
using OrderManagementSystem.Common.Models;

namespace OrderManagementSystem.Features.Customers.GetAllCustomer
{

    public static class GetAllCustomersEndpoint
    {
        public static RouteGroupBuilder MapGetAllCustomersEndpoint(this RouteGroupBuilder group)
        {
            group.MapGet("", async (
              [FromQuery]  int? PagenNumber,
              [FromQuery]  int?  pageSize,
              GetAllCustomersHandler handler,
              HttpContext httpContext,
              CancellationToken cancellationToken) =>
            {
                var request = new GetAllCustomerRequest
                {
                    PageNumber = PagenNumber ?? 1,
                    PageSize = pageSize ?? 10
                };

                var result = await handler.HandleAsync(request, cancellationToken);
                
                return result.ToEndpointResult(httpContext);

            })
          .MapToApiVersion(new ApiVersion(1, 0))
          .WithName("GetAllCustomers")
          .Produces<PagedResult<GetAllCustomersResponse>>(StatusCodes.Status200OK)
          .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
          .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
          .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
          .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
          .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

            return group;
        }
    }
}
