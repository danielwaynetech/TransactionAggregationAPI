using TransactionAggregatorAPI.DataAccess;
using TransactionAggregatorAPI.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TransactionAggregatorAPI.Domain.Services;

/// <summary>
/// Service interface for audit logging
/// </summary>
public interface IAuditService
{
    Task LogAsync(Guid entityId, string entityType, string action, object? oldValues, object? newValues, string performedBy, string? ipAddress = null, string? userAgent = null);
    Task<IEnumerable<AuditLogEntity>> GetAuditLogsAsync(Guid entityId, string entityType, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for audit logging all data changes
/// </summary>
public class AuditService : IAuditService
{
    private readonly TransactionDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(TransactionDbContext context, ILogger<AuditService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Log an audit entry
    /// </summary>
    public async Task LogAsync(
        Guid entityId,
        string entityType,
        string action,
        object? oldValues,
        object? newValues,
        string performedBy,
        string? ipAddress = null,
        string? userAgent = null)
    {
        try
        {
            var auditLog = new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                EntityType = entityType,
                Action = action,
                PerformedBy = performedBy,
                PerformedAt = DateTime.UtcNow,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Audit log created: {Action} on {EntityType} {EntityId} by {PerformedBy}",
                action, entityType, entityId, performedBy);
        }
        catch (Exception ex)
        {
            // Don't throw - audit logging should not break the main flow
            _logger.LogError(ex, "Failed to create audit log for {EntityType} {EntityId}", entityType, entityId);
        }
    }

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    public async Task<IEnumerable<AuditLogEntity>> GetAuditLogsAsync(
        Guid entityId,
        string entityType,
        CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs
            .Where(a => a.EntityId == entityId && a.EntityType == entityType)
            .OrderByDescending(a => a.PerformedAt)
            .ToListAsync(cancellationToken);
    }
}
