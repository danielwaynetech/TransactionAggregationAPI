using TransactionAggregatorAPI.Domain.Contracts;
using TransactionAggregatorAPI.Domain.Exceptions;
using TransactionAggregatorAPI.Domain.Models;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Transactions;
using Transaction = TransactionAggregatorAPI.Domain.Models.Transaction;
using TransactionStatus = TransactionAggregatorAPI.Domain.Models.TransactionStatus;

namespace TransactionAggregatorAPI.Domain.Services;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _repository;
    private readonly IEnumerable<IDataSourceService> _dataSources;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        ITransactionRepository repository,
        IEnumerable<IDataSourceService> dataSources,
        ILogger<TransactionService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _dataSources = dataSources ?? throw new ArgumentNullException(nameof(dataSources));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving transaction with ID: {TransactionId}", id);
        return await _repository.GetByIdAsync(id, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetCustomerTransactionsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new InvalidCustomerIdException(customerId);

        _logger.LogInformation("Retrieving transactions for customer: {CustomerId}", customerId);
        return await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(
        string customerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new InvalidCustomerIdException(customerId);

        if (startDate > endDate)
            throw new InvalidDateRangeException(startDate, endDate);

        _logger.LogInformation(
            "Retrieving transactions for customer: {CustomerId} from {StartDate} to {EndDate}",
            customerId, startDate, endDate);

        return await _repository.GetByCustomerIdAndDateRangeAsync(
            customerId, startDate, endDate, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByCategoryAsync(
        string customerId,
        TransactionCategory category,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new InvalidCustomerIdException(customerId);

        _logger.LogInformation(
            "Retrieving {Category} transactions for customer: {CustomerId}",
            category, customerId);

        return await _repository.GetByCategoryAsync(customerId, category, cancellationToken);
    }

    public async Task<TransactionSummary> GetTransactionSummaryAsync(
        string customerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new InvalidCustomerIdException(customerId);

        if (startDate > endDate)
            throw new InvalidDateRangeException(startDate, endDate);

        _logger.LogInformation(
            "Generating transaction summary for customer: {CustomerId} from {StartDate} to {EndDate}",
            customerId, startDate, endDate);

        var transactions = await _repository.GetByCustomerIdAndDateRangeAsync(
            customerId, startDate, endDate, cancellationToken);

        var transactionList = transactions.ToList();

        var summary = new TransactionSummary
        {
            CustomerId = customerId,
            StartDate = startDate,
            EndDate = endDate,
            TransactionCount = transactionList.Count
        };

        // Calculate totals
        summary.TotalIncome = transactionList
            .Where(t => t.Type == TransactionType.Credit && t.Status == TransactionStatus.Completed)
            .Sum(t => t.Amount);

        summary.TotalExpenses = transactionList
            .Where(t => t.Type == TransactionType.Debit && t.Status == TransactionStatus.Completed)
            .Sum(t => t.Amount);

        summary.NetAmount = summary.TotalIncome - summary.TotalExpenses;

        // Calculate category breakdown
        var categoryGroups = transactionList
            .Where(t => t.Status == TransactionStatus.Completed)
            .GroupBy(t => t.Category)
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                Percentage = 0 // Will calculate below
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToList();

        var totalCategoryAmount = categoryGroups.Sum(c => c.TotalAmount);
        if (totalCategoryAmount > 0)
        {
            foreach (var category in categoryGroups)
            {
                category.Percentage = Math.Round((category.TotalAmount / totalCategoryAmount) * 100, 2);
            }
        }

        summary.CategoryBreakdown = categoryGroups;

        return summary;
    }

    public async Task<CustomerSummary> GetCustomerSummaryAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new InvalidCustomerIdException(customerId);

        _logger.LogInformation("Generating customer summary for: {CustomerId}", customerId);

        var transactions = await _repository.GetByCustomerIdAsync(customerId, cancellationToken);
        var transactionList = transactions.ToList();

        var summary = new CustomerSummary
        {
            CustomerId = customerId,
            TotalTransactions = transactionList.Count,
            TotalAccounts = transactionList.Select(t => t.AccountId).Distinct().Count(),
            LastTransactionDate = transactionList.Any()
                ? transactionList.Max(t => t.TransactionDate)
                : null
        };

        var completedTransactions = transactionList
            .Where(t => t.Status == TransactionStatus.Completed)
            .ToList();

        var totalIncome = completedTransactions
            .Where(t => t.Type == TransactionType.Credit)
            .Sum(t => t.Amount);

        var totalExpenses = completedTransactions
            .Where(t => t.Type == TransactionType.Debit)
            .Sum(t => t.Amount);

        summary.TotalBalance = totalIncome - totalExpenses;

        return summary;
    }

    public async Task<Transaction> CreateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        if (string.IsNullOrWhiteSpace(transaction.CustomerId))
            throw new InvalidCustomerIdException(transaction.CustomerId);

        if (string.IsNullOrWhiteSpace(transaction.AccountId))
            throw new InvalidAccountIdException(transaction.AccountId);

        if (transaction.Amount < 0)
            throw new InvalidTransactionDataException("Amount cannot be negative");

        _logger.LogInformation("Creating new transaction for customer: {CustomerId}", transaction.CustomerId);

        transaction.Id = Guid.NewGuid();
        transaction.CreatedAt = DateTime.UtcNow;

        return await _repository.AddAsync(transaction, cancellationToken);
    }

    public async Task AggregateTransactionsFromSourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting transaction aggregation from {SourceCount} data sources",
            _dataSources.Count());

        var allTransactions = new List<Transaction>();

        foreach (var dataSource in _dataSources)
        {
            try
            {
                _logger.LogInformation("Fetching transactions from source: {SourceName}", dataSource.SourceName);
                var transactions = await dataSource.FetchTransactionsAsync(cancellationToken);

                var transactionList = transactions.ToList();
                allTransactions.AddRange(transactionList);

                _logger.LogInformation("Retrieved {Count} transactions from {SourceName}",
                    transactionList.Count, dataSource.SourceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transactions from source: {SourceName}",
                    dataSource.SourceName);
                throw new DataSourceException(dataSource.SourceName, "Failed to fetch transactions", ex);
            }
        }

        if (allTransactions.Any())
        {
            _logger.LogInformation("Adding {Count} transactions to repository", allTransactions.Count);
            await _repository.AddRangeAsync(allTransactions, cancellationToken);
        }

        _logger.LogInformation("Transaction aggregation completed. Total transactions: {Count}",
            allTransactions.Count);
    }
}
