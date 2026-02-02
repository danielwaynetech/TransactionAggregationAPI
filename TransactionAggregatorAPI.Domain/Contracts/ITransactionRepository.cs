using TransactionAggregatorAPI.Domain.Models;
using System.Transactions;
using Transaction = TransactionAggregatorAPI.Domain.Models.Transaction;

namespace TransactionAggregatorAPI.Domain.Contracts;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByCustomerIdAndDateRangeAsync(
        string customerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetByCategoryAsync(
        string customerId,
        TransactionCategory category,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, string? deletedBy = null, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
