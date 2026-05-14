using Microsoft.AspNetCore.Mvc;
using OrderManagementSystem.Common.Errors;
using OrderManagementSystem.Common.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace OrderManagementSystem.Common.Api;

public static class ResultEndpointExtensions
{
    public static IResult ToEndpointResult<TValue>(
        this Result<TValue> result,
        HttpContext httpContext)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        return result.Match<TValue, IResult>(
            onSuccess: value => HttpResults.Ok(value),
            onFailure: errors => MapErrors(errors, httpContext));
    }


    public static IResult ToNoContentEndpointResult(
        this Result result,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        return result.Match<IResult>(
            onSuccess: () => HttpResults.NoContent(),
            onFailure: errors => MapErrors(errors, httpContext));
    }
    public static IResult ToCreatedEndpointResult<TValue>(
        this Result<TValue> result,
        HttpContext httpContext)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        return result.Match<TValue, IResult>(
            onSuccess: value => HttpResults.Json(
                value,
                statusCode: StatusCodes.Status201Created),
            onFailure: errors => MapErrors(errors, httpContext));
    }

    private static IResult MapErrors(
        IReadOnlyList<Error> errors,
        HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(errors);
        ArgumentNullException.ThrowIfNull(httpContext);

        if (errors.Count == 0)
        {
            throw new InvalidOperationException("Cannot map an empty error collection.");
        }

        var firstError = errors[0];

        return firstError.Type switch
        {
            ErrorType.Validation =>
                HttpResults.BadRequest(ToValidationProblemDetails(errors, httpContext)),

            ErrorType.NotFound =>
                HttpResults.NotFound(ToProblemDetails(
                    errors,
                    httpContext,
                    StatusCodes.Status404NotFound,
                    "Not Found")),

            ErrorType.Conflict =>
                HttpResults.Conflict(ToProblemDetails(
                    errors,
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict")),

            ErrorType.Unauthorized =>
                HttpResults.Unauthorized(),

            ErrorType.Forbidden =>
                HttpResults.Problem(ToProblemDetails(
                    errors,
                    httpContext,
                    StatusCodes.Status403Forbidden,
                    "Forbidden")),

            _ =>
                HttpResults.BadRequest(ToProblemDetails(
                    errors,
                    httpContext,
                    StatusCodes.Status400BadRequest,
                    "Request Failed"))
        };
    }

    private static ValidationProblemDetails ToValidationProblemDetails(
        IReadOnlyList<Error> errors,
        HttpContext httpContext)
    {
        var dictionary = errors
            .GroupBy(error => error.Field ?? string.Empty)
            .ToDictionary(
                group => string.IsNullOrWhiteSpace(group.Key) ? "General" : group.Key,
                group => group.Select(error => error.Description).ToArray());

        return new ValidationProblemDetails(dictionary)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Instance = httpContext.Request.Path
        };
    }

    private static ProblemDetails ToProblemDetails(
        IReadOnlyList<Error> errors,
        HttpContext httpContext,
        int statusCode,
        string title)
    {
        var firstError = errors[0];

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = string.Join(" | ", errors.Select(error => error.Description)),
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["errorCode"] = firstError.Code;

        return problemDetails;
    }
}