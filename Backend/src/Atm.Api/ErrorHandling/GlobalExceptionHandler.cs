using Atm.Application.Exceptions;
using Atm.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Atm.Api.ErrorHandling;

/// <summary>
/// Single place where exceptions become HTTP responses (RFC 7807 ProblemDetails).
/// Endpoints and services never build error responses themselves.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException ex => ValidationProblem(ex),
            NotFoundException ex => Problem(StatusCodes.Status404NotFound, "Resource not found", ex.Message, "not_found"),
            DomainException ex => Problem(StatusCodes.Status422UnprocessableEntity, "Operation not allowed", ex.Message, ex.Code),
            ConcurrencyConflictException ex => Problem(StatusCodes.Status409Conflict, "Concurrency conflict", ex.Message, "concurrency_conflict"),
            BadHttpRequestException ex => Problem(ex.StatusCode, "Bad request", "The request is malformed.", "bad_request"),
            _ => null,
        };

        if (problem is null)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
            problem = Problem(StatusCodes.Status500InternalServerError, "Unexpected error",
                "An unexpected error occurred. Please try again later.", "internal_error");
        }
        else
        {
            logger.LogInformation("Request {Method} {Path} rejected: {Reason}",
                httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string detail, string code) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Extensions = { ["code"] = code },
    };

    private static ValidationProblemDetails ValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = errors.Values.SelectMany(m => m).FirstOrDefault() ?? "One or more validation errors occurred.",
            Extensions = { ["code"] = "validation_failed" },
        };
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
