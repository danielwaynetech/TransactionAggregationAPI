using TransactionAggregatorAPI.Domain.Models;

namespace TransactionAggregatorAPI.Domain.Contracts;

public interface ITransactionService
{
    Task<Transaction?> GetTransactionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetCustomerTransactionsAsync(
        string customerId,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetTransactionsByDateRangeAsync(
        string customerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetTransactionsByCategoryAsync(
        string customerId,
        TransactionCategory category,
        CancellationToken cancellationToken = default);
    Task<TransactionSummary> GetTransactionSummaryAsync(
        string customerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
    Task<CustomerSummary> GetCustomerSummaryAsync(
        string customerId,
        CancellationToken cancellationToken = default);
    Task<Transaction> CreateTransactionAsync(
        Transaction transaction,
        CancellationToken cancellationToken = default);
    Task AggregateTransactionsFromSourcesAsync(CancellationToken cancellationToken = default);
}
