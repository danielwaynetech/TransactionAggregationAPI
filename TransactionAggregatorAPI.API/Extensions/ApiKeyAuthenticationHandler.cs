using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace TransactionAggregatorAPI.API.Extensions;

/// <summary>
/// API Key authentication scheme
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";
}

/// <summary>
/// Handles API key authentication
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, loggerFactory, encoder)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<ApiKeyAuthenticationHandler>();
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if API key header exists
        if (!Request.Headers.TryGetValue(Options.ApiKeyHeaderName, out var extractedApiKey))
        {
            _logger.LogWarning("API key not provided in header {HeaderName}", Options.ApiKeyHeaderName);
            return Task.FromResult(AuthenticateResult.Fail("API Key was not provided"));
        }

        // Get valid API keys from configuration
        var validApiKeys = _configuration.GetSection("Authentication:ApiKeys").Get<List<ApiKeyConfig>>()
            ?? new List<ApiKeyConfig>();

        var apiKeyConfig = validApiKeys.FirstOrDefault(k => k.Key == extractedApiKey.ToString());

        if (apiKeyConfig == null)
        {
            _logger.LogWarning("Invalid API key provided: {ApiKey}", MaskApiKey(extractedApiKey.ToString()));
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        // Check if key is enabled
        if (!apiKeyConfig.IsEnabled)
        {
            _logger.LogWarning("Disabled API key attempted: {ApiKeyName}", apiKeyConfig.Name);
            return Task.FromResult(AuthenticateResult.Fail("API Key is disabled"));
        }

        _logger.LogInformation("Successful authentication for API key: {ApiKeyName}", apiKeyConfig.Name);

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, apiKeyConfig.Name),
            new Claim("ApiKeyName", apiKeyConfig.Name)
        };

        // Add role claims
        if (apiKeyConfig.Roles != null)
        {
            claims.AddRange(apiKeyConfig.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        }

        var identity = new ClaimsIdentity(claims, Options.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Options.Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length < 8)
            return "***";

        return $"{apiKey[..4]}...{apiKey[^4..]}";
    }
}

/// <summary>
/// Configuration for a single API key
/// </summary>
public class ApiKeyConfig
{
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public List<string>? Roles { get; set; }
}
