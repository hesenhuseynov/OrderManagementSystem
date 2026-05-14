using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Errors;
using OrderManagementSystem.Common.Results;

namespace OrderManagementSystem.Common.Api
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected ActionResult<T> HandleResult<T>(Result<T> result)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }
            return MapErrors<T>(result.Errors);
        }

        
        protected ActionResult HandleResult(Result result)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            return MapErrors(result.Errors);
        }

        protected ActionResult<T> HandleCreatedResult<T>(Result<T> result)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(result);

            if (result.IsSuccess)
            {
                return StatusCode(StatusCodes.Status201Created, result.Value);
            }

            return MapErrors<T>(result.Errors);
        }

        
        private ActionResult MapErrors(IReadOnlyList<Error> errors)
        {
            ArgumentNullException.ThrowIfNull(errors);

            if (errors.Count == 0)
            {
                throw new InvalidOperationException("Cannot map an empty error collection.");
            }

            var firstError = errors[0];

            return firstError.Type switch
            {
                ErrorType.Validation => BadRequest(ToValidationProblemDetails(errors)),

                ErrorType.NotFound => NotFound(
                    ToProblemDetails(
                        errors,
                        StatusCodes.Status404NotFound,
                        "Not Found")),

                ErrorType.Conflict => Conflict(
                    ToProblemDetails(
                        errors,
                        StatusCodes.Status409Conflict,
                        "Conflict")),

                ErrorType.Unauthorized => Unauthorized(),

                ErrorType.Forbidden => StatusCode(
                    StatusCodes.Status403Forbidden,
                    ToProblemDetails(
                        errors,
                        StatusCodes.Status403Forbidden,
                        "Forbidden")),
             
                _ => BadRequest(
                    ToProblemDetails(
                        errors,
                        StatusCodes.Status400BadRequest,
                        "Request Failed"))
            };
        }

        private ActionResult<T> MapErrors<T>(IReadOnlyList<Error> errors)
            where T : notnull
        {
            ArgumentNullException.ThrowIfNull(errors);


            if (errors.Count == 0)
            {
                throw new InvalidOperationException("Cannot map an empty error collection.");
            }

            var firstError = errors[0];

            return firstError.Type switch
            {
                ErrorType.Validation => BadRequest(ToValidationProblemDetails(errors)),

                ErrorType.NotFound => NotFound(
                    ToProblemDetails(
                        errors,
                        StatusCodes.Status404NotFound,
                        "Not Found")),

                ErrorType.Conflict => Conflict(
                    ToProblemDetails(
                        errors,
                        StatusCodes.Status409Conflict,
                        "Conflict")),

                ErrorType.Unauthorized => Unauthorized(),

                ErrorType.Forbidden => StatusCode(
                    StatusCodes.Status403Forbidden,
                    ToProblemDetails(
                        errors,
                        StatusCodes.Status403Forbidden,
                        "Forbidden")),

                _ => BadRequest(
                    ToProblemDetails(
                        errors,
                        StatusCodes.Status400BadRequest,
                        "Request Failed"))
            };
        }

      
        private ValidationProblemDetails ToValidationProblemDetails(IReadOnlyList<Error> errors)
        {
            var dictionary = errors
                .GroupBy(x => x.Field ?? string.Empty)
                .ToDictionary(
                    g => string.IsNullOrWhiteSpace(g.Key) ? "General" : g.Key,
                    g => g.Select(x => x.Description).ToArray());

            return new ValidationProblemDetails(dictionary)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed",
                Instance = HttpContext.Request.Path
            };
        }

        private ProblemDetails ToProblemDetails(
            IReadOnlyList<Error> errors,
            int statusCode,
            string title)
        {
            var firstError = errors[0]; 

             var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = string.Join(" | ", errors.Select(x => x.Description)),
                Instance = HttpContext.Request.Path
            };

            problemDetails.Extensions["errorCode"] = firstError.Code;

            return problemDetails;
        }
    }
}
