using TransactionAggregatorAPI.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace TransactionAggregatorAPI.API.Extensions;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            TransactionNotFoundException => (HttpStatusCode.NotFound, "Transaction Not Found"),
            InvalidCustomerIdException => (HttpStatusCode.BadRequest, "Invalid Customer ID"),
            InvalidAccountIdException => (HttpStatusCode.BadRequest, "Invalid Account ID"),
            InvalidDateRangeException => (HttpStatusCode.BadRequest, "Invalid Date Range"),
            InvalidTransactionDataException => (HttpStatusCode.BadRequest, "Invalid Transaction Data"),
            DataSourceException => (HttpStatusCode.ServiceUnavailable, "Data Source Error"),
            ArgumentNullException => (HttpStatusCode.BadRequest, "Bad Request"),
            ArgumentException => (HttpStatusCode.BadRequest, "Bad Request"),
            _ => (HttpStatusCode.InternalServerError, "Internal Server Error")
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}

public class ProblemDetails
{
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Instance { get; set; } = string.Empty;
}
