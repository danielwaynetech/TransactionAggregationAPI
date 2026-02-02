using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;

namespace TransactionAggregatorAPI.API.Controllers;

/// <summary>
/// Health check and metrics endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<HealthController> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public HealthController(
        HealthCheckService healthCheckService,
        ILogger<HealthController> logger)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthCheckResponse>> Get()
    {
        try
        {
            var report = await _healthCheckService.CheckHealthAsync();

            var response = new HealthCheckResponse
            {
                Status = report.Status.ToString(),
                TotalDuration = report.TotalDuration,
                Checks = report.Entries.Select(e => new HealthCheckItem
                {
                    Name = e.Key,
                    Status = e.Value.Status.ToString(),
                    Duration = e.Value.Duration,
                    Description = e.Value.Description,
                    Data = e.Value.Data
                }).ToList()
            };

            var statusCode = report.Status == HealthStatus.Healthy
                ? StatusCodes.Status200OK
                : StatusCodes.Status503ServiceUnavailable;

            return StatusCode(statusCode, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new HealthCheckResponse
            {
                Status = "Unhealthy",
                Checks = new List<HealthCheckItem>
                {
                    new HealthCheckItem
                    {
                        Name = "HealthCheck",
                        Status = "Unhealthy",
                        Description = ex.Message
                    }
                }
            });
        }
    }

    /// <summary>
    /// Liveness probe - indicates if the application is running
    /// </summary>
    /// <returns>200 OK if alive</returns>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new { status = "Alive", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Readiness probe - indicates if the application is ready to serve requests
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var statusCode = report.Status == HealthStatus.Healthy
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;

        return StatusCode(statusCode, new { status = report.Status.ToString() });
    }

    /// <summary>
    /// Application metrics endpoint
    /// </summary>
    /// <returns>Application metrics</returns>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(MetricsResponse), StatusCodes.Status200OK)]
    public ActionResult<MetricsResponse> Metrics()
    {
        var process = Process.GetCurrentProcess();

        var metrics = new MetricsResponse
        {
            Application = new ApplicationMetrics
            {
                Name = "Financial Aggregator API",
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                Uptime = DateTime.UtcNow - _startTime,
                StartTime = _startTime
            },
            System = new SystemMetrics
            {
                ProcessorCount = Environment.ProcessorCount,
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                VirtualMemory = process.VirtualMemorySize64,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount
            }
        };

        return Ok(metrics);
    }
}

/// <summary>
/// Health check response model
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public List<HealthCheckItem> Checks { get; set; } = new();
}

/// <summary>
/// Individual health check item
/// </summary>
public class HealthCheckItem
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public string? Description { get; set; }
    public IReadOnlyDictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Application metrics response
/// </summary>
public class MetricsResponse
{
    public ApplicationMetrics Application { get; set; } = new();
    public SystemMetrics System { get; set; } = new();
}

/// <summary>
/// Application-level metrics
/// </summary>
public class ApplicationMetrics
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
    public DateTime StartTime { get; set; }
}

/// <summary>
/// System-level metrics
/// </summary>
public class SystemMetrics
{
    public int ProcessorCount { get; set; }
    public long WorkingSet { get; set; }
    public long PrivateMemory { get; set; }
    public long VirtualMemory { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
}
