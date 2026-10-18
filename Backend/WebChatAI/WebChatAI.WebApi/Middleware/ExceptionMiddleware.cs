using System.Net;
using System.Text.Json;
using FluentValidation;
using WebChatAI.Application.Common;

namespace WebChatAI.WebApi.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, IHostEnvironment env, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        if (exception is ValidationException validationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var errors = validationException.Errors.Select(e => e.ErrorMessage).ToList();
            var response = ApiResponse.FailureResult("Validation error", errors);

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
            return;
        }

        var correlationId = context.TraceIdentifier;
        _logger.LogError(exception, "An unhandled exception occurred during request execution. CorrelationId: {CorrelationId}", correlationId);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        var errorDetails = _env.IsDevelopment()
            ? new List<string> { exception.Message, exception.StackTrace ?? string.Empty }
            : new List<string> { $"İç sunucu hatası meydana geldi. Referans takip kodu: {correlationId}" };

        var genericResponse = ApiResponse.FailureResult(
            "An unexpected server error occurred. Please try again later.",
            errorDetails
        );

        var generalJson = JsonSerializer.Serialize(genericResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(generalJson);
    }
}
