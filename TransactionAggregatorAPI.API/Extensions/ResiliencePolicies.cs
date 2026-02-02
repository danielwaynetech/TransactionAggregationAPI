using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace TransactionAggregatorAPI.API.Extensions;

/// <summary>
/// Resilience policies for HTTP calls and database operations
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry policy with exponential backoff (3 attempts)
    /// </summary>
    public static AsyncRetryPolicy CreateRetryPolicy(ILogger logger)
    {
        return Policy
            .Handle<Exception>(ex => !(ex is ArgumentException || ex is ArgumentNullException))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "Retry {RetryCount} after {DelaySeconds}s due to: {ExceptionMessage}",
                        retryCount,
                        timeSpan.TotalSeconds,
                        exception.Message);
                });
    }

    /// <summary>
    /// Circuit breaker policy - opens after 5 consecutive failures
    /// </summary>
    public static AsyncCircuitBreakerPolicy CreateCircuitBreakerPolicy(ILogger logger)
    {
        return Policy
            .Handle<Exception>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, duration) =>
                {
                    logger.LogError(
                        exception,
                        "Circuit breaker opened for {DurationSeconds}s due to: {ExceptionMessage}",
                        duration.TotalSeconds,
                        exception.Message);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker half-open, testing...");
                });
    }

    /// <summary>
    /// Timeout policy - 30 seconds for operations
    /// </summary>
    public static AsyncTimeoutPolicy CreateTimeoutPolicy(ILogger logger, TimeSpan? timeout = null)
    {
        var timeoutDuration = timeout ?? TimeSpan.FromSeconds(30);

        return Policy
            .TimeoutAsync(
                timeout: timeoutDuration,
                timeoutStrategy: TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timeSpan, task) =>
                {
                    logger.LogWarning(
                        "Operation timed out after {TimeoutSeconds}s",
                        timeSpan.TotalSeconds);
                    return Task.CompletedTask;
                });
    }

    /// <summary>
    /// Combined policy: Timeout -> Retry -> Circuit Breaker
    /// </summary>
    public static IAsyncPolicy CreateCombinedPolicy(ILogger logger, TimeSpan? timeout = null)
    {
        var timeoutPolicy = CreateTimeoutPolicy(logger, timeout);
        var retryPolicy = CreateRetryPolicy(logger);
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy(logger);

        // Wrap policies: innermost (timeout) to outermost (circuit breaker)
        return Policy.WrapAsync(circuitBreakerPolicy, retryPolicy, timeoutPolicy);
    }
}

/// <summary>
/// Policy registry for managing different policies
/// </summary>
public class PolicyRegistry
{
    private readonly ILogger<PolicyRegistry> _logger;
    private readonly Dictionary<string, IAsyncPolicy> _policies;

    public PolicyRegistry(ILogger<PolicyRegistry> logger)
    {
        _logger = logger;
        _policies = new Dictionary<string, IAsyncPolicy>
        {
            ["DatabasePolicy"] = ResiliencePolicies.CreateCombinedPolicy(_logger, TimeSpan.FromSeconds(30)),
            ["CachePolicy"] = ResiliencePolicies.CreateCombinedPolicy(_logger, TimeSpan.FromSeconds(10)),
            ["DataSourcePolicy"] = ResiliencePolicies.CreateCombinedPolicy(_logger, TimeSpan.FromSeconds(60))
        };
    }

    public IAsyncPolicy GetPolicy(string name)
    {
        if (_policies.TryGetValue(name, out var policy))
        {
            return policy;
        }

        _logger.LogWarning("Policy {PolicyName} not found, returning default policy", name);
        return _policies["DatabasePolicy"];
    }
}
