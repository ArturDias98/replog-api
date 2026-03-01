using FluentValidation;
using replog_shared.Models.Responses;

namespace replog_api.Middleware;

public class ValidationExceptionHandler(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var errors = string.Join("; ", ex.Errors.Select(e => e.ErrorMessage));
            var response = new ErrorResponse
            {
                Error = "validation_error",
                Message = errors
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
