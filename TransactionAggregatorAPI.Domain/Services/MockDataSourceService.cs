using TransactionAggregatorAPI.Domain.Contracts;
using TransactionAggregatorAPI.Domain.Models;
using System.Data.Common;
using System.Transactions;
using Transaction = TransactionAggregatorAPI.Domain.Models.Transaction;
using TransactionStatus = TransactionAggregatorAPI.Domain.Models.TransactionStatus;

namespace TransactionAggregatorAPI.Domain.Services;

public class BankADataSource : IDataSourceService
{
    public string SourceName => "BankA";

    public Task<IEnumerable<Transaction>> FetchTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST001",
                AccountId = "ACC-BANKA-001",
                Amount = 1250.50m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-5),
                Type = TransactionType.Credit,
                Category = TransactionCategory.Income,
                Description = "Salary Deposit",
                MerchantName = "ACME Corp",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-BANKA-001",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST001",
                AccountId = "ACC-BANKA-001",
                Amount = 45.99m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-4),
                Type = TransactionType.Debit,
                Category = TransactionCategory.Groceries,
                Description = "Grocery Shopping",
                MerchantName = "Whole Foods Market",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-BANKA-002",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST001",
                AccountId = "ACC-BANKA-001",
                Amount = 89.50m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-3),
                Type = TransactionType.Debit,
                Category = TransactionCategory.Dining,
                Description = "Restaurant Payment",
                MerchantName = "The Blue Restaurant",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-BANKA-003",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST002",
                AccountId = "ACC-BANKA-002",
                Amount = 2100.00m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-6),
                Type = TransactionType.Credit,
                Category = TransactionCategory.Income,
                Description = "Payroll Deposit",
                MerchantName = "Tech Solutions Inc",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-BANKA-004",
                CreatedAt = DateTime.UtcNow
            }
        };

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }
}

public class BankBDataSource : IDataSourceService
{
    public string SourceName => "BankB";

    public Task<IEnumerable<Transaction>> FetchTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST001",
                AccountId = "ACC-BANKB-001",
                Amount = 150.00m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-7),
                Type = TransactionType.Debit,
                Category = TransactionCategory.Utilities,
                Description = "Electric Bill",
                MerchantName = "City Power Company",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-BANKB-001",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST001",
                AccountId = "ACC-BANKB-001",
                Amount = 75.00m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-6),
                Type = TransactionType.Debit,
                Category = TransactionCategory.Transportation,
                Description = "Gas Station",
                MerchantName = "Shell",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-BANKB-002",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST003",
                AccountId = "ACC-BANKB-003",
                Amount = 3500.00m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-5),
                Type = TransactionType.Credit,
                Category = TransactionCategory.Income,
                Description = "Monthly Salary",
                MerchantName = "Global Industries",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-BANKB-003",
                CreatedAt = DateTime.UtcNow
            }
        };

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }
}

public class CreditCardDataSource : IDataSourceService
{
    public string SourceName => "CreditCardProvider";

    public Task<IEnumerable<Transaction>> FetchTransactionsAsync(CancellationToken cancellationToken = default)
    {
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST001",
                AccountId = "CC-001",
                Amount = 299.99m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-2),
                Type = TransactionType.Debit,
                Category = TransactionCategory.Shopping,
                Description = "Online Purchase",
                MerchantName = "Amazon",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-CC-001",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST001",
                AccountId = "CC-001",
                Amount = 120.00m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Type = TransactionType.Debit,
                Category = TransactionCategory.Entertainment,
                Description = "Movie Theater",
                MerchantName = "Cineplex",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-CC-002",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST002",
                AccountId = "CC-002",
                Amount = 450.00m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-3),
                Type = TransactionType.Debit,
                Category = TransactionCategory.Travel,
                Description = "Flight Booking",
                MerchantName = "Delta Airlines",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-CC-003",
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = "CUST002",
                AccountId = "CC-002",
                Amount = 55.00m,
                Currency = "USD",
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                Type = TransactionType.Debit,
                Category = TransactionCategory.Dining,
                Description = "Restaurant",
                MerchantName = "Italian Bistro",
                Status = TransactionStatus.Completed,
                SourceSystem = SourceName,
                Reference = "REF-CC-004",
                CreatedAt = DateTime.UtcNow
            }
        };

        return Task.FromResult<IEnumerable<Transaction>>(transactions);
    }
}
