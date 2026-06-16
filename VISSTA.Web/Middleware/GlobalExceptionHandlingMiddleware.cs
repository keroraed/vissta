namespace VISSTA.Web.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request was cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled request failure.");
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
