using System.Net;
using System.Text.Json;
using AppointmentBooking.Api.Exceptions;

namespace AppointmentBooking.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            await WriteError(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ConflictException ex)
        {
            await WriteError(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (UnauthorizedAppException ex)
        {
            await WriteError(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteError(context, HttpStatusCode.InternalServerError, "Internal server error: " + ex.Message);
        }
    }

    private static async Task WriteError(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var body = JsonSerializer.Serialize(new
        {
            timestamp = DateTime.UtcNow,
            status = (int)status,
            message
        });

        await context.Response.WriteAsync(body);
    }
}
