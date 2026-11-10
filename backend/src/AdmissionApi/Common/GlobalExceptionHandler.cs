using Microsoft.AspNetCore.Diagnostics;

namespace AdmissionApi.Common;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, body) = exception switch
        {
            AppException app => (app.StatusCode, new ErrorResponse(app.Code, app.Message)),
            BadHttpRequestException => (400, new ErrorResponse("VALIDATION_ERROR", "The request is not valid.")),
            _ => (500, new ErrorResponse("SERVER_ERROR", "Something went wrong on the server. Please try again."))
        };

        if (status >= 500)
            _logger.LogError(exception, "Unhandled error");

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(body, ct);
        return true;
    }
}
