using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransactionAggregatorAPI.DataAccess.Entities;

/// <summary>
/// Audit log entity for tracking all changes to transactions
/// </summary>
[Table("AuditLogs")]
public class AuditLogEntity
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    [Key]
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// ID of the entity that was modified
    /// </summary>
    [Required]
    public Guid EntityId { get; set; }

    /// <summary>
    /// Type of entity (e.g., "Transaction")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Action performed (Created, Updated, Deleted, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// User or system that performed the action
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PerformedBy { get; set; } = string.Empty;

    /// <summary>
    /// When the action was performed
    /// </summary>
    [Required]
    public DateTime PerformedAt { get; set; }

    /// <summary>
    /// Previous values (JSON)
    /// </summary>
    [Column(TypeName = "text")]
    public string? OldValues { get; set; }

    /// <summary>
    /// New values (JSON)
    /// </summary>
    [Column(TypeName = "text")]
    public string? NewValues { get; set; }

    /// <summary>
    /// IP address of the request
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the request
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
}
