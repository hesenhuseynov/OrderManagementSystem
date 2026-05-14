using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Api;
using OrderManagementSystem.Common.Models;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.AccessControl;

namespace OrderManagementSystem.Features.Customers.GetAllCustomer
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/customers")]
    [Tags("Customers")]
    public class GetAllCustomersEndpoint:ApiControllerBase
    {   
        private readonly GetAllCustomersHandler _handler;
        
        public GetAllCustomersEndpoint(GetAllCustomersHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler); 
            _handler = handler;  
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<GetAllCustomersResponse>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResult<GetAllCustomersResponse>>>GetAll(
            [FromQuery]  GetAllCustomerRequest  request, CancellationToken cancellationToken 
            )
        {
            var result = await _handler.HandleAsync(request, cancellationToken);
            return HandleResult(result);
        }
    }
}
