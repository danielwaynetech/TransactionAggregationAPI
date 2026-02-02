using TransactionAggregatorAPI.Domain.Models;
using TransactionAggregatorAPI.Domain.Services;
using TransactionAggregatorAPI.Domain.Contracts;
using TransactionAggregatorAPI.Domain.Exceptions;
using TransactionAggregatorAPI.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace TransactionAggregatorAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionService transactionService,
        ICacheService cacheService,
        ILogger<TransactionsController> logger)
    {
        _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get a transaction by ID (with caching)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("GET request for transaction {TransactionId}", id);

        // Try to get from cache first
        var cacheKey = $"transaction:{id}";
        var cachedTransaction = await _cacheService.GetAsync<TransactionResponse>(cacheKey, cancellationToken);

        if (cachedTransaction != null)
        {
            _logger.LogInformation("Transaction {TransactionId} retrieved from cache", id);
            return Ok(cachedTransaction);
        }

        var transaction = await _transactionService.GetTransactionByIdAsync(id, cancellationToken);

        if (transaction == null)
        {
            throw new TransactionNotFoundException(id);
        }

        var response = MapToResponse(transaction);

        // Cache for 10 minutes
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get all transactions for a customer with OData support (filtering, sorting, pagination)
    /// </summary>
    [HttpGet("customer/{customerId}")]
    [EnableQuery(PageSize = 50, MaxTop = 100)]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IQueryable<TransactionResponse>>> GetCustomerTransactions(
        string customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new InvalidCustomerIdException(customerId);
        }

        _logger.LogInformation("GET request for customer {CustomerId} transactions", customerId);

        // Try cache first
        var cacheKey = $"customer:{customerId}:transactions";
        var cachedTransactions = await _cacheService.GetAsync<List<TransactionResponse>>(cacheKey, cancellationToken);

        if (cachedTransactions != null)
        {
            _logger.LogInformation("Transactions for customer {CustomerId} retrieved from cache", customerId);
            return Ok(cachedTransactions.AsQueryable());
        }

        var transactions = await _transactionService.GetCustomerTransactionsAsync(customerId, cancellationToken);
        var response = transactions.Select(MapToResponse).ToList();

        // Cache for 5 minutes
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

        return Ok(response.AsQueryable());
    }

    /// <summary>
    /// Get transactions by date range for a customer
    /// </summary>
    [HttpGet("customer/{customerId}/date-range")]
    [EnableQuery(PageSize = 50, MaxTop = 100)]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IQueryable<TransactionResponse>>> GetTransactionsByDateRange(
        string customerId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new InvalidCustomerIdException(customerId);
        }

        if (startDate > endDate)
        {
            throw new InvalidDateRangeException(startDate, endDate);
        }

        _logger.LogInformation(
            "GET request for customer {CustomerId} transactions from {StartDate} to {EndDate}",
            customerId, startDate, endDate);

        var transactions = await _transactionService.GetTransactionsByDateRangeAsync(
            customerId, startDate, endDate, cancellationToken);

        var response = transactions.Select(MapToResponse);

        return Ok(response.AsQueryable());
    }

    /// <summary>
    /// Get transactions by category for a customer
    /// </summary>
    [HttpGet("customer/{customerId}/category/{category}")]
    [EnableQuery(PageSize = 50, MaxTop = 100)]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IQueryable<TransactionResponse>>> GetTransactionsByCategory(
        string customerId,
        TransactionCategory category,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new InvalidCustomerIdException(customerId);
        }

        _logger.LogInformation(
            "GET request for customer {CustomerId} transactions in category {Category}",
            customerId, category);

        // Cache key includes category
        var cacheKey = $"customer:{customerId}:category:{category}:transactions";
        var cachedTransactions = await _cacheService.GetAsync<List<TransactionResponse>>(cacheKey, cancellationToken);

        if (cachedTransactions != null)
        {
            return Ok(cachedTransactions.AsQueryable());
        }

        var transactions = await _transactionService.GetTransactionsByCategoryAsync(
            customerId, category, cancellationToken);

        var response = transactions.Select(MapToResponse).ToList();

        // Cache for 5 minutes
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(5), cancellationToken);

        return Ok(response.AsQueryable());
    }

    /// <summary>
    /// Create a new transaction (invalidates cache)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        _logger.LogInformation("POST request to create transaction for customer {CustomerId}", request.CustomerId);

        var transaction = MapToDomain(request);
        var createdTransaction = await _transactionService.CreateTransactionAsync(transaction, cancellationToken);
        var response = MapToResponse(createdTransaction);

        // Invalidate customer cache
        await _cacheService.RemoveAsync($"customer:{request.CustomerId}:transactions", cancellationToken);
        await _cacheService.RemoveAsync($"customer:{request.CustomerId}:summary", cancellationToken);

        return CreatedAtAction(
            nameof(GetTransaction),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Get transaction summary for a customer in a date range (with caching)
    /// </summary>
    [HttpGet("customer/{customerId}/summary")]
    [ProducesResponseType(typeof(TransactionSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionSummaryResponse>> GetTransactionSummary(
        string customerId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new InvalidCustomerIdException(customerId);
        }

        if (startDate > endDate)
        {
            throw new InvalidDateRangeException(startDate, endDate);
        }

        _logger.LogInformation(
            "GET request for customer {CustomerId} summary from {StartDate} to {EndDate}",
            customerId, startDate, endDate);

        // Cache key includes date range
        var cacheKey = $"customer:{customerId}:summary:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
        var cachedSummary = await _cacheService.GetAsync<TransactionSummaryResponse>(cacheKey, cancellationToken);

        if (cachedSummary != null)
        {
            _logger.LogInformation("Summary for customer {CustomerId} retrieved from cache", customerId);
            return Ok(cachedSummary);
        }

        var summary = await _transactionService.GetTransactionSummaryAsync(
            customerId, startDate, endDate, cancellationToken);

        var response = MapToSummaryResponse(summary);

        // Cache for 15 minutes
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(15), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get customer summary (with caching)
    /// </summary>
    [HttpGet("customer/{customerId}/customer-summary")]
    [ProducesResponseType(typeof(CustomerSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerSummaryResponse>> GetCustomerSummary(
        string customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            throw new InvalidCustomerIdException(customerId);
        }

        _logger.LogInformation("GET request for customer {CustomerId} summary", customerId);

        var cacheKey = $"customer:{customerId}:summary";
        var cachedSummary = await _cacheService.GetAsync<CustomerSummaryResponse>(cacheKey, cancellationToken);

        if (cachedSummary != null)
        {
            return Ok(cachedSummary);
        }

        var summary = await _transactionService.GetCustomerSummaryAsync(customerId, cancellationToken);
        var response = MapToCustomerSummaryResponse(summary);

        // Cache for 10 minutes
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10), cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Aggregate transactions from all data sources
    /// </summary>
    [HttpPost("aggregate")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> AggregateTransactions(CancellationToken cancellationToken)
    {
        _logger.LogInformation("POST request to aggregate transactions from data sources");

        await _transactionService.AggregateTransactionsFromSourcesAsync(cancellationToken);

        // Note: In production, you'd want to invalidate all customer caches here
        // This would require a more sophisticated cache implementation with key tracking

        return Accepted(new { Message = "Transaction aggregation completed successfully" });
    }

    #region Mapping Methods

    private static TransactionResponse MapToResponse(Transaction transaction)
    {
        return new TransactionResponse
        {
            Id = transaction.Id,
            CustomerId = transaction.CustomerId,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            TransactionDate = transaction.TransactionDate,
            Type = transaction.Type.ToString(),
            Category = transaction.Category.ToString(),
            Description = transaction.Description,
            MerchantName = transaction.MerchantName,
            Status = transaction.Status.ToString(),
            SourceSystem = transaction.SourceSystem,
            Reference = transaction.Reference,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }

    private static Transaction MapToDomain(CreateTransactionRequest request)
    {
        return new Transaction
        {
            CustomerId = request.CustomerId,
            AccountId = request.AccountId,
            Amount = request.Amount,
            Currency = request.Currency,
            TransactionDate = request.TransactionDate,
            Type = request.Type,
            Category = request.Category,
            Description = request.Description,
            MerchantName = request.MerchantName,
            Status = request.Status,
            SourceSystem = request.SourceSystem,
            Reference = request.Reference
        };
    }

    private static TransactionSummaryResponse MapToSummaryResponse(TransactionSummary summary)
    {
        return new TransactionSummaryResponse
        {
            CustomerId = summary.CustomerId,
            StartDate = summary.StartDate,
            EndDate = summary.EndDate,
            TotalIncome = summary.TotalIncome,
            TotalExpenses = summary.TotalExpenses,
            NetAmount = summary.NetAmount,
            TransactionCount = summary.TransactionCount,
            CategoryBreakdown = summary.CategoryBreakdown.Select(c => new CategorySummaryResponse
            {
                Category = c.Category.ToString(),
                TotalAmount = c.TotalAmount,
                TransactionCount = c.TransactionCount,
                Percentage = c.Percentage
            }).ToList()
        };
    }

    private static CustomerSummaryResponse MapToCustomerSummaryResponse(CustomerSummary summary)
    {
        return new CustomerSummaryResponse
        {
            CustomerId = summary.CustomerId,
            TotalAccounts = summary.TotalAccounts,
            TotalBalance = summary.TotalBalance,
            TotalTransactions = summary.TotalTransactions,
            LastTransactionDate = summary.LastTransactionDate
        };
    }

    #endregion
}
