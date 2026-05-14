using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Reflection;

namespace OrderManagementSystem.Middleware
{
    public class GlobalExcpetionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExcpetionMiddleware> _logger;

        public GlobalExcpetionMiddleware(RequestDelegate next, ILogger<GlobalExcpetionMiddleware> logger)
        {
            ArgumentNullException.ThrowIfNull(next);
            ArgumentNullException.ThrowIfNull(logger);

            _next = next;
            _logger = logger;
        }
       
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occured");

                await WriteProblemDetailsAsync(
                context,
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: "An unexpected error occurred.");
            }
        }
        
        public static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string detail)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = statusCode, 
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
