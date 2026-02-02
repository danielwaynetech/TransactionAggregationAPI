using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionAggregatorAPI.DataAccess.Entities;

/// <summary>
/// Database entity representing a financial transaction
/// </summary>
[Table("Transactions")]
public class TransactionEntity
{
    /// <summary>
    /// Unique identifier for the transaction
    /// </summary>
    [Key]
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// Customer identifier
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Account identifier
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column(TypeName = "varchar(100)")]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 999999999.99)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217)
    /// </summary>
    [Required]
    [MaxLength(3)]
    [MinLength(3)]
    [Column(TypeName = "varchar(3)")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Date and time when the transaction occurred
    /// </summary>
    [Required]
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Transaction type (0=Debit, 1=Credit)
    /// </summary>
    [Required]
    public int Type { get; set; }

    /// <summary>
    /// Transaction category
    /// </summary>
    [Required]
    public int Category { get; set; }

    /// <summary>
    /// Transaction description
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Merchant or vendor name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string MerchantName { get; set; } = string.Empty;

    /// <summary>
    /// Transaction status (0=Pending, 1=Completed, 2=Failed, 3=Cancelled)
    /// </summary>
    [Required]
    public int Status { get; set; }

    /// <summary>
    /// Source system that provided this transaction
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SourceSystem { get; set; } = string.Empty;

    /// <summary>
    /// External reference number
    /// </summary>
    [MaxLength(100)]
    public string? Reference { get; set; }

    /// <summary>
    /// Soft delete flag
    /// </summary>
    [Required]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When the record was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Who created the record
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// When the record was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Who last updated the record
    /// </summary>
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// When the record was deleted (soft delete)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Who deleted the record (soft delete)
    /// </summary>
    [MaxLength(100)]
    public string? DeletedBy { get; set; }
}
