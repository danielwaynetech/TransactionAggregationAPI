using TransactionAggregatorAPI.Domain.Models;
using System.Transactions;
using Transaction = TransactionAggregatorAPI.Domain.Models.Transaction;

namespace TransactionAggregatorAPI.Domain.Contracts;

public interface IDataSourceService
{
    string SourceName { get; }
    Task<IEnumerable<Transaction>> FetchTransactionsAsync(CancellationToken cancellationToken = default);
}
