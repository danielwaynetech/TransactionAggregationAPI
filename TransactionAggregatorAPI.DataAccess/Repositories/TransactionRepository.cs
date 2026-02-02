using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TransactionAggregatorAPI.DataAccess.Entities;
using TransactionAggregatorAPI.Domain.Contracts;
using TransactionAggregatorAPI.Domain.Models;

namespace TransactionAggregatorAPI.DataAccess.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly TransactionDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(
        TransactionDbContext context,
        IMapper mapper,
        ILogger<TransactionRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        return entity != null ? _mapper.Map<Transaction>(entity) : null;
    }

    public async Task<IEnumerable<Transaction>> GetByCustomerIdAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Transactions
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<Transaction>>(entities);
    }

    public async Task<IEnumerable<Transaction>> GetByCustomerIdAndDateRangeAsync(
        string customerId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Transactions
            .Where(t => t.CustomerId == customerId
                && t.TransactionDate >= startDate
                && t.TransactionDate <= endDate)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<Transaction>>(entities);
    }

    public async Task<IEnumerable<Transaction>> GetByCategoryAsync(
        string customerId,
        TransactionCategory category,
        CancellationToken cancellationToken = default)
    {
        var categoryInt = (int)category;

        var entities = await _context.Transactions
            .Where(t => t.CustomerId == customerId && t.Category == categoryInt)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<Transaction>>(entities);
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.Transactions
            .OrderByDescending(t => t.TransactionDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<Transaction>>(entities);
    }

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var entity = _mapper.Map<TransactionEntity>(transaction);

        await _context.Transactions.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} added successfully", entity.Id);

        return _mapper.Map<Transaction>(entity);
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        var entities = _mapper.Map<IEnumerable<TransactionEntity>>(transactions).ToList();

        await _context.Transactions.AddRangeAsync(entities, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added {Count} transactions", entities.Count);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var entity = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transaction.Id, cancellationToken);

        if (entity == null)
        {
            throw new InvalidOperationException($"Transaction {transaction.Id} not found");
        }

        _mapper.Map(transaction, entity);
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} updated successfully", entity.Id);
    }

    public async Task DeleteAsync(Guid id, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        // Use IgnoreQueryFilters to find the transaction even if already soft-deleted
        var entity = await _context.Transactions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (entity != null && !entity.IsDeleted)
        {
            // Soft delete - set IsDeleted flag instead of removing from database
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = deletedBy ?? "system";

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Transaction {TransactionId} soft deleted successfully by {DeletedBy}",
                id,
                entity.DeletedBy);
        }
        else if (entity?.IsDeleted == true)
        {
            _logger.LogWarning("Transaction {TransactionId} is already deleted", id);
        }
        else
        {
            _logger.LogWarning("Transaction {TransactionId} not found for deletion", id);
        }
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Transactions.CountAsync(cancellationToken);
    }
}