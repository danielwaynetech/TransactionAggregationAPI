using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionAggregatorAPI.DataAccess;
using TransactionAggregatorAPI.DataAccess.Entities;
using TransactionAggregatorAPI.DataAccess.Repositories;
using TransactionAggregatorAPI.Domain.Models;
using Xunit;

namespace TransactionAggregatorAPI.Tests;

/// <summary>
/// Integration tests for TransactionRepository
/// Uses in-memory database for testing
/// </summary>
public class TransactionRepositoryTests : IDisposable
{
    private readonly TransactionDbContext _context;
    private readonly TransactionRepository _repository;
    private readonly Mock<ILogger<TransactionRepository>> _mockLogger;

    public TransactionRepositoryTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<TransactionDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TransactionDbContext(options);
        _mockLogger = new Mock<ILogger<TransactionRepository>>();

        // Create AutoMapper mock (repository uses it)
        var mockMapper = CreateMockMapper();
        _repository = new TransactionRepository(_context, mockMapper, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTransaction_WhenExists()
    {
        // Arrange
        var entity = CreateTestEntity(Guid.NewGuid(), "CUST001", 100m);
        await _context.Transactions.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.CustomerId.Should().Be("CUST001");
        result.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenSoftDeleted()
    {
        // Arrange - Create a soft-deleted transaction
        var entity = CreateTestEntity(Guid.NewGuid(), "CUST001", 100m);
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = "test-user";

        await _context.Transactions.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(entity.Id);

        // Assert
        result.Should().BeNull("soft-deleted records should not be returned");
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ShouldReturnAllNonDeletedTransactions()
    {
        // Arrange
        var customerId = "CUST001";
        var transaction1 = CreateTestEntity(Guid.NewGuid(), customerId, 100m);
        var transaction2 = CreateTestEntity(Guid.NewGuid(), customerId, 200m);
        var deletedTransaction = CreateTestEntity(Guid.NewGuid(), customerId, 300m);
        deletedTransaction.IsDeleted = true;

        await _context.Transactions.AddRangeAsync(transaction1, transaction2, deletedTransaction);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByCustomerIdAsync(customerId);

        // Assert
        result.Should().HaveCount(2, "soft-deleted transaction should be excluded");
        result.Should().AllSatisfy(t =>
        {
            t.CustomerId.Should().Be(customerId);
        });
    }

    [Fact]
    public async Task GetByCustomerIdAndDateRangeAsync_ShouldFilterCorrectly()
    {
        // Arrange
        var customerId = "CUST001";
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);

        var inRange = CreateTestEntity(Guid.NewGuid(), customerId, 100m);
        inRange.TransactionDate = new DateTime(2024, 6, 15);

        var beforeRange = CreateTestEntity(Guid.NewGuid(), customerId, 200m);
        beforeRange.TransactionDate = new DateTime(2023, 12, 31);

        var afterRange = CreateTestEntity(Guid.NewGuid(), customerId, 300m);
        afterRange.TransactionDate = new DateTime(2025, 1, 1);

        await _context.Transactions.AddRangeAsync(inRange, beforeRange, afterRange);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByCustomerIdAndDateRangeAsync(customerId, startDate, endDate);

        // Assert
        result.Should().HaveCount(1);
        result.First().TransactionDate.Should().Be(new DateTime(2024, 6, 15));
    }

    [Fact]
    public async Task GetByCategoryAsync_ShouldReturnOnlyMatchingCategory()
    {
        // Arrange
        var customerId = "CUST001";
        var groceries1 = CreateTestEntity(Guid.NewGuid(), customerId, 50m);
        groceries1.Category = (int)TransactionCategory.Groceries;

        var groceries2 = CreateTestEntity(Guid.NewGuid(), customerId, 75m);
        groceries2.Category = (int)TransactionCategory.Groceries;

        var dining = CreateTestEntity(Guid.NewGuid(), customerId, 100m);
        dining.Category = (int)TransactionCategory.Dining;

        await _context.Transactions.AddRangeAsync(groceries1, groceries2, dining);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByCategoryAsync(customerId, TransactionCategory.Groceries);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.Category.Should().Be(TransactionCategory.Groceries));
    }

    [Fact]
    public async Task AddAsync_ShouldCreateTransaction()
    {
        // Arrange
        var transaction = CreateTestDomainModel(Guid.NewGuid(), "CUST001", 150m);

        // Act
        var result = await _repository.AddAsync(transaction);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);

        var saved = await _context.Transactions.FindAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.Amount.Should().Be(150m);
        saved.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task AddRangeAsync_ShouldAddMultipleTransactions()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            CreateTestDomainModel(Guid.NewGuid(), "CUST001", 100m),
            CreateTestDomainModel(Guid.NewGuid(), "CUST002", 200m),
            CreateTestDomainModel(Guid.NewGuid(), "CUST003", 300m)
        };

        // Act
        await _repository.AddRangeAsync(transactions);

        // Assert
        var count = await _context.Transactions.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifyTransaction()
    {
        // Arrange
        var entity = CreateTestEntity(Guid.NewGuid(), "CUST001", 100m);
        await _context.Transactions.AddAsync(entity);
        await _context.SaveChangesAsync();

        var transaction = CreateTestDomainModel(entity.Id, "CUST001", 200m);

        // Act
        await _repository.UpdateAsync(transaction);

        // Assert
        var updated = await _context.Transactions.FindAsync(entity.Id);
        updated.Should().NotBeNull();
        updated!.Amount.Should().Be(200m);
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldSetIsDeletedToTrue()
    {
        // Arrange
        var entity = CreateTestEntity(Guid.NewGuid(), "CUST001", 100m);
        await _context.Transactions.AddAsync(entity);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(entity.Id);

        // Assert - Use IgnoreQueryFilters to see deleted records
        var deleted = await _context.Transactions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == entity.Id);

        deleted.Should().NotBeNull("record should still exist in database");
        deleted!.IsDeleted.Should().BeTrue("IsDeleted flag should be set");
        deleted.DeletedAt.Should().NotBeNull("DeletedAt should be set");

        // Verify it's excluded from normal queries
        var normalQuery = await _repository.GetByIdAsync(entity.Id);
        normalQuery.Should().BeNull("soft-deleted records should not appear in normal queries");
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenTransactionNotExists()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _repository.DeleteAsync(nonExistentId);

        // Assert
        await act.Should().NotThrowAsync("deleting non-existent record should be idempotent");
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var transaction1 = CreateTestEntity(Guid.NewGuid(), "CUST001", 100m);
        var transaction2 = CreateTestEntity(Guid.NewGuid(), "CUST002", 200m);
        var deletedTransaction = CreateTestEntity(Guid.NewGuid(), "CUST003", 300m);
        deletedTransaction.IsDeleted = true;

        await _context.Transactions.AddRangeAsync(transaction1, transaction2, deletedTransaction);
        await _context.SaveChangesAsync();

        // Act
        var count = await _repository.GetCountAsync();

        // Assert
        count.Should().Be(2, "soft-deleted transactions should not be counted");
    }

    [Fact]
    public async Task GetAllAsync_ShouldSupportPagination()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            var transaction = CreateTestEntity(Guid.NewGuid(), $"CUST{i:000}", i * 10m);
            await _context.Transactions.AddAsync(transaction);
        }
        await _context.SaveChangesAsync();

        // Act - Get second page, 10 items per page
        var result = await _repository.GetAllAsync(pageNumber: 2, pageSize: 10);

        // Assert
        result.Should().HaveCount(10);
    }

    [Fact]
    public async Task GlobalQueryFilter_ShouldExcludeSoftDeletedByDefault()
    {
        // Arrange
        var active = CreateTestEntity(Guid.NewGuid(), "CUST001", 100m);
        var deleted = CreateTestEntity(Guid.NewGuid(), "CUST002", 200m);
        deleted.IsDeleted = true;

        await _context.Transactions.AddRangeAsync(active, deleted);
        await _context.SaveChangesAsync();

        // Act
        var allFromContext = await _context.Transactions.ToListAsync();
        var allFromRepo = await _repository.GetAllAsync();

        // Assert
        allFromContext.Should().HaveCount(1, "global query filter should exclude soft-deleted");
        allFromRepo.Should().HaveCount(1, "repository should respect global query filter");
    }

    [Fact]
    public async Task IgnoreQueryFilters_ShouldIncludeSoftDeleted()
    {
        // Arrange
        var active = CreateTestEntity(Guid.NewGuid(), "CUST001", 100m);
        var deleted = CreateTestEntity(Guid.NewGuid(), "CUST002", 200m);
        deleted.IsDeleted = true;

        await _context.Transactions.AddRangeAsync(active, deleted);
        await _context.SaveChangesAsync();

        // Act
        var allIncludingDeleted = await _context.Transactions
            .IgnoreQueryFilters()
            .ToListAsync();

        // Assert
        allIncludingDeleted.Should().HaveCount(2, "IgnoreQueryFilters should include soft-deleted records");
        allIncludingDeleted.Should().Contain(t => t.IsDeleted == true);
    }

    #region Helper Methods

    private TransactionEntity CreateTestEntity(Guid id, string customerId, decimal amount)
    {
        return new TransactionEntity
        {
            Id = id,
            CustomerId = customerId,
            AccountId = "ACC001",
            Amount = amount,
            Currency = "USD",
            TransactionDate = DateTime.UtcNow,
            Type = 0,
            Category = (int)TransactionCategory.Groceries,
            Description = "Test transaction",
            MerchantName = "Test Merchant",
            Status = 1,
            SourceSystem = "Test",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "test-user"
        };
    }

    private Transaction CreateTestDomainModel(Guid id, string customerId, decimal amount)
    {
        return new Transaction
        {
            Id = id,
            CustomerId = customerId,
            AccountId = "ACC001",
            Amount = amount,
            Currency = "USD",
            TransactionDate = DateTime.UtcNow,
            Type = TransactionType.Debit,
            Category = TransactionCategory.Groceries,
            Description = "Test transaction",
            MerchantName = "Test Merchant",
            Status = TransactionStatus.Completed,
            SourceSystem = "Test",
            CreatedAt = DateTime.UtcNow
        };
    }

    private AutoMapper.IMapper CreateMockMapper()
    {
        var config = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TransactionEntity, Transaction>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (TransactionType)src.Type))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => (TransactionCategory)src.Category))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (TransactionStatus)src.Status));

            cfg.CreateMap<Transaction, TransactionEntity>()
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => (int)src.Type))
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => (int)src.Category))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => (int)src.Status));
        });

        return config.CreateMapper();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}