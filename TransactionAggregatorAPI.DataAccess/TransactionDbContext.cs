using Microsoft.EntityFrameworkCore;
using TransactionAggregatorAPI.DataAccess.Entities;

namespace TransactionAggregatorAPI.DataAccess;

/// <summary>
/// Database context for the Financial Aggregator application
/// </summary>
public class TransactionDbContext : DbContext
{
    public TransactionDbContext(DbContextOptions<TransactionDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Transactions table
    /// </summary>
    public DbSet<TransactionEntity> Transactions { get; set; } = null!;

    /// <summary>
    /// Audit logs table
    /// </summary>
    public DbSet<AuditLogEntity> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Transaction entity
        modelBuilder.Entity<TransactionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Global query filter to exclude soft-deleted records
            entity.HasQueryFilter(e => !e.IsDeleted);

            // Indexes for performance
            entity.HasIndex(e => e.CustomerId)
                .HasDatabaseName("IX_Transactions_CustomerId");

            entity.HasIndex(e => e.AccountId)
                .HasDatabaseName("IX_Transactions_AccountId");

            entity.HasIndex(e => e.TransactionDate)
                .HasDatabaseName("IX_Transactions_TransactionDate");

            entity.HasIndex(e => e.Category)
                .HasDatabaseName("IX_Transactions_Category");

            entity.HasIndex(e => e.IsDeleted)
                .HasDatabaseName("IX_Transactions_IsDeleted");

            entity.HasIndex(e => new { e.CustomerId, e.TransactionDate })
                .HasDatabaseName("IX_Transactions_CustomerId_TransactionDate");

            // Precision for decimal
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            // Default values
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Indexes
            entity.HasIndex(e => e.EntityId)
                .HasDatabaseName("IX_AuditLogs_EntityId");

            entity.HasIndex(e => e.EntityType)
                .HasDatabaseName("IX_AuditLogs_EntityType");

            entity.HasIndex(e => e.PerformedAt)
                .HasDatabaseName("IX_AuditLogs_PerformedAt");

            entity.HasIndex(e => new { e.EntityId, e.EntityType })
                .HasDatabaseName("IX_AuditLogs_EntityId_EntityType");
        });
    }
}
