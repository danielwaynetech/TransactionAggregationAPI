namespace TransactionAggregatorAPI.Domain.Models;

public class Transaction
{
    public Guid Id { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum TransactionType
{
    Debit,
    Credit
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}

public enum TransactionCategory
{
    Unknown,
    Groceries,
    Dining,
    Transportation,
    Entertainment,
    Shopping,
    Utilities,
    Healthcare,
    Education,
    Travel,
    Income,
    Transfer,
    Investment,
    Insurance,
    Housing,
    PersonalCare,
    Professional,
    Other
}
