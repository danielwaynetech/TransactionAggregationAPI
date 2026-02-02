namespace TransactionAggregatorAPI.Domain.Models;

public class TransactionSummary
{
    public string CustomerId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetAmount { get; set; }
    public int TransactionCount { get; set; }
    public List<CategorySummary> CategoryBreakdown { get; set; } = new();
}

public class CategorySummary
{
    public TransactionCategory Category { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
    public decimal Percentage { get; set; }
}

public class CustomerSummary
{
    public string CustomerId { get; set; } = string.Empty;
    public int TotalAccounts { get; set; }
    public decimal TotalBalance { get; set; }
    public int TotalTransactions { get; set; }
    public DateTime? LastTransactionDate { get; set; }
}
