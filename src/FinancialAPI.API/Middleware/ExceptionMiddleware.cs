using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace FinancialAPI.API.Middleware;

/// <summary>
/// Global exception handler — catches all unhandled exceptions and returns
/// a structured RFC 7807 ProblemDetails JSON response instead of HTML error pages.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        _next   = next;
        _logger = logger;
        _env    = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, title) = ex switch
        {
            ArgumentException or InvalidOperationException
                => (HttpStatusCode.BadRequest, "Bad Request"),
            KeyNotFoundException
                => (HttpStatusCode.NotFound, "Not Found"),
            UnauthorizedAccessException
                => (HttpStatusCode.Unauthorized, "Unauthorized"),
            _   => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        _logger.LogError(ex,
            "Unhandled exception. TraceId: {TraceId}, Path: {Path}, StatusCode: {StatusCode}",
            context.TraceIdentifier, context.Request.Path, (int)statusCode);

        var problem = new ProblemDetails
        {
            Status   = (int)statusCode,
            Title    = title,
            Detail   = _env.IsDevelopment() ? ex.Message : "An error occurred. Please try again.",
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"]  = context.TraceIdentifier,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        if (_env.IsDevelopment())
            problem.Extensions["stackTrace"] = ex.StackTrace;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = (int)statusCode;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
