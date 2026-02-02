using TransactionAggregatorAPI.Domain.Models;

namespace TransactionAggregatorAPI.API.Models;

public class TransactionResponse
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime TransactionDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTransactionRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public TransactionCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public string MerchantName { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; }
    public string SourceSystem { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

public class TransactionSummaryResponse
{
    public string CustomerId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }
    public List<CategorySummaryResponse> CategoryBreakdown { get; set; } = new();
}

public class CategorySummaryResponse
{
    public string Category { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

public class CustomerSummaryResponse
{
    public string CustomerId { get; set; } = string.Empty;
    public int TotalAccounts { get; set; }
    public decimal TotalBalance { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}
