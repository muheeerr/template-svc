using Utility.Helpers.Common;
using Utility.CustomHTTP;
using Utility.Logger;

namespace projectname.Host.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        IHostEnvironment environment,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _environment = environment;
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
            _logger.LogError(ex, "An unhandled exception occurred. Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);

            // If response has already started, we can't modify it
            if (context.Response.HasStarted)
            {
                _logger.LogError("Response has already started, cannot modify response. Exception: {Exception}", ex.Message);
                throw; // Re-throw to let the default handler deal with it
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Clear any existing response content
        context.Response.Clear();

        context.Response.ContentType = "application/json";

        var response = GetErrorResponse(exception);
        context.Response.StatusCode = response.StatusCode;

        await response.ExecuteAsync(context);
    }

    private ApiResponseModel GetErrorResponse(Exception exception)
    {
        // Determine status code based on exception type
        int statusCode = GetStatusCode(exception);
        string message = GetErrorMessage(exception);
        object? exceptionDetails = GetExceptionDetails(exception);

        return ApiResponseHelper.Failure(
            message: message,
            statusCode: statusCode,
            exception: exceptionDetails
        );
    }

    private int GetStatusCode(Exception exception)
    {
        // Map common exception types to appropriate HTTP status codes
        return exception switch
        {
            ArgumentNullException or ArgumentException => HTTPStatusCode400.BadRequest,
            UnauthorizedAccessException => HTTPStatusCode400.Unauthorized,
            KeyNotFoundException or FileNotFoundException => HTTPStatusCode400.NotFound,
            InvalidOperationException => HTTPStatusCode400.BadRequest,
            NotSupportedException => HTTPStatusCode400.BadRequest,
            TimeoutException => HTTPStatusCode500.GatewayTimeout,
            _ => HTTPStatusCode500.InternalServerError
        };
    }

    private string GetErrorMessage(Exception exception)
    {
        // In production, return generic messages to avoid leaking sensitive information
        // In development, return more detailed messages
        if (_environment.IsDevelopment())
        {
            return $"An error occurred: {exception.Message}";
        }

        // Return generic messages for production
        return exception switch
        {
            ArgumentNullException or ArgumentException => "Invalid request parameters.",
            UnauthorizedAccessException => "You are not authorized to perform this action.",
            KeyNotFoundException or FileNotFoundException => "The requested resource was not found.",
            InvalidOperationException => "The requested operation cannot be performed.",
            NotSupportedException => "The requested operation is not supported.",
            TimeoutException => "The request timed out. Please try again later.",
            BadHttpRequestException => "Invalid Request",
            _ => "An internal server error occurred. Please try again later or contact support."
        };
    }

    private object? GetExceptionDetails(Exception exception)
    {
        // Only include exception details in development
        if (_environment.IsDevelopment())
        {
            var details = new List<object>();

            var currentException = exception;
            int depth = 0;
            const int maxDepth = 5; // Prevent infinite recursion

            while (currentException != null && depth < maxDepth)
            {
                details.Add(new
                {
                    Type = currentException.GetType().Name,
                    Message = currentException.Message,
                    StackTrace = currentException.StackTrace
                });

                currentException = currentException.InnerException;
                depth++;
            }

            return details;
        }

        // In production, return null or minimal information
        return null;
    }
}


