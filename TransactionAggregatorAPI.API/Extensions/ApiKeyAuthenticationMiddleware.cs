using Microsoft.Extensions.Primitives;

namespace TransactionAggregatorAPI.API.Extensions;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
    private const string API_KEY_HEADER = "X-API-Key";

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check and metrics endpoints
        if (context.Request.Path.StartsWithSegments("/health") ||
            context.Request.Path.StartsWithSegments("/metrics") ||
            context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(API_KEY_HEADER, out StringValues extractedApiKey))
        {
            _logger.LogWarning("API Key missing from request. Path: {Path}, IP: {IP}",
                context.Request.Path, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 401,
                title = "Unauthorized",
                detail = "API Key is missing. Include X-API-Key header."
            });
            return;
        }

        var validApiKeys = _configuration.GetSection("ApiKeys").Get<List<string>>() ?? new List<string>();

        if (!validApiKeys.Contains(extractedApiKey.ToString()))
        {
            _logger.LogWarning("Invalid API Key attempted. Path: {Path}, IP: {IP}",
                context.Request.Path, context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new
            {
                status = 401,
                title = "Unauthorized",
                detail = "Invalid API Key."
            });
            return;
        }

        _logger.LogInformation("Authenticated request. Path: {Path}, IP: {IP}",
            context.Request.Path, context.Connection.RemoteIpAddress);

        await _next(context);
    }
}

public static class ApiKeyAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
    }
}
