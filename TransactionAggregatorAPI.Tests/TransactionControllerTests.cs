using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TransactionAggregatorAPI.API.Controllers;
using TransactionAggregatorAPI.API.Models;
using TransactionAggregatorAPI.Domain.Contracts;
using TransactionAggregatorAPI.Domain.Exceptions;
using TransactionAggregatorAPI.Domain.Models;
using Xunit;

namespace TransactionAggregatorAPI.Tests;

public class TransactionsControllerTests
{
    private readonly Mock<ITransactionService> _mockService;
    private readonly Mock<ILogger<TransactionsController>> _mockLogger;
    private readonly TransactionsController _controller;
    private readonly Mock<ICacheService> _mockCache;

    public TransactionsControllerTests()
    {
        _mockService = new Mock<ITransactionService>();
        _mockCache = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<TransactionsController>>();
        _controller = new TransactionsController(_mockService.Object, _mockCache.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetTransaction_ShouldReturnOk_WhenTransactionExists()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var mockTransaction = CreateMockTransaction(transactionId, "CUST001", 100m);

        _mockService
            .Setup(s => s.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction);

        // Act
        var result = await _controller.GetTransaction(transactionId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var response = okResult!.Value as TransactionResponse;
        response.Should().NotBeNull();
        response!.Id.Should().Be(transactionId);
        response.CustomerId.Should().Be("CUST001");
        response.Amount.Should().Be(100m);

        _mockService.Verify(s => s.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTransaction_ShouldReturnTransactionNotFound_WhenTransactionDoesNotExist()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        _mockService
            .Setup(s => s.GetTransactionByIdAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        Func<Task> act = () => _controller.GetTransaction(transactionId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TransactionNotFoundException>()
            .WithMessage("*Transaction*not found*");
    }

    [Fact]
    public async Task GetCustomerTransactions_ShouldReturnInvalidCustomerIdExceptiont_WhenCustomerIdIsEmpty()
    {
        // Act
        var act = () => _controller.GetCustomerTransactions(string.Empty, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidCustomerIdException>()
            .WithMessage("*Customer ID*invalid*");
    }

    [Fact]
    public async Task GetCustomerTransactions_ShouldReturnOk_WithTransactions()
    {
        // Arrange
        var customerId = "CUST001";
        var mockTransactions = new List<Transaction>
        {
            CreateMockTransaction(Guid.NewGuid(), customerId, 100m),
            CreateMockTransaction(Guid.NewGuid(), customerId, 200m)
        };

        _mockService
            .Setup(s => s.GetCustomerTransactionsAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransactions);

        // Act
        var result = await _controller.GetCustomerTransactions(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;

        var response = okResult!.Value as IEnumerable<TransactionResponse>;
        response.Should().NotBeNull();
        response.Should().HaveCount(2);
        response.Should().AllSatisfy(t => t.CustomerId.Should().Be(customerId));
    }

    [Fact]
    public async Task GetTransactionsByDateRange_ShouldReturnInvalidDateRange_WhenStartDateAfterEndDate()
    {
        // Arrange
        var customerId = "CUST001";
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(-1);

        // Act
        Func<Task> act = () => _controller.GetTransactionsByDateRange(
            customerId, startDate, endDate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidDateRangeException>()
            .WithMessage("Invalid date range*");
    }

    [Fact]
    public async Task GetTransactionsByDateRange_ShouldReturnOk_WhenDatesAreValid()
    {
        // Arrange
        var customerId = "CUST001";
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var mockTransactions = new List<Transaction>
        {
            CreateMockTransaction(Guid.NewGuid(), customerId, 100m, DateTime.UtcNow.AddDays(-5))
        };

        _mockService
            .Setup(s => s.GetTransactionsByDateRangeAsync(customerId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransactions);

        // Act
        var result = await _controller.GetTransactionsByDateRange(customerId, startDate, endDate, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CreateTransaction_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateTransactionRequest
        {
            CustomerId = "CUST001",
            AccountId = "ACC001",
            Amount = 100m,
            Type = TransactionType.Debit,
            Category = TransactionCategory.Groceries,
            Status = TransactionStatus.Completed,
            Description = "Test transaction",
            MerchantName = "Test Merchant",
            TransactionDate = DateTime.UtcNow,
            SourceSystem = "TestSystem"
        };

        var createdTransaction = CreateMockTransaction(Guid.NewGuid(), request.CustomerId, request.Amount);

        _mockService
            .Setup(s => s.CreateTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdTransaction);

        // Act
        var result = await _controller.CreateTransaction(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;

        var response = createdResult!.Value as TransactionResponse;
        response.Should().NotBeNull();
        response!.CustomerId.Should().Be("CUST001");
        response.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task CreateTransaction_ShouldReturnBadRequest_WhenRequestIsNull()
    {
        // Act
        var result = await _controller.CreateTransaction(null!, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTransactionSummary_ShouldReturnOk_WithSummary()
    {
        // Arrange
        var customerId = "CUST001";
        var startDate = DateTime.UtcNow.AddDays(-30);
        var endDate = DateTime.UtcNow;

        var mockSummary = new TransactionSummary
        {
            CustomerId = customerId,
            StartDate = startDate,
            EndDate = endDate,
            TotalIncome = 1000m,
            TotalExpenses = 300m,
            NetAmount = 700m,
            TransactionCount = 5,
            CategoryBreakdown = new List<CategorySummary>
            {
                new() { Category = TransactionCategory.Income, TotalAmount = 1000m, TransactionCount = 1, Percentage = 50 },
                new() { Category = TransactionCategory.Groceries, TotalAmount = 300m, TransactionCount = 4, Percentage = 50 }
            }
        };

        _mockService
            .Setup(s => s.GetTransactionSummaryAsync(customerId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSummary);

        // Act
        var result = await _controller.GetTransactionSummary(customerId, startDate, endDate, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;

        var response = okResult!.Value as TransactionSummaryResponse;
        response.Should().NotBeNull();
        response!.CustomerId.Should().Be(customerId);
        response.TotalIncome.Should().Be(1000m);
        response.TotalExpenses.Should().Be(300m);
        response.NetAmount.Should().Be(700m);
    }

    [Fact]
    public async Task GetCustomerSummary_ShouldReturnOk_WithSummary()
    {
        // Arrange
        var customerId = "CUST001";
        var mockSummary = new CustomerSummary
        {
            CustomerId = customerId,
            TotalAccounts = 2,
            TotalBalance = 5000m,
            TotalTransactions = 10,
            LastTransactionDate = DateTime.UtcNow
        };

        _mockService
            .Setup(s => s.GetCustomerSummaryAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockSummary);

        // Act
        var result = await _controller.GetCustomerSummary(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;

        var response = okResult!.Value as CustomerSummaryResponse;
        response.Should().NotBeNull();
        response!.CustomerId.Should().Be(customerId);
        response.TotalAccounts.Should().Be(2);
        response.TotalBalance.Should().Be(5000m);
    }

    [Fact]
    public async Task AggregateTransactions_ShouldReturnAccepted()
    {
        // Arrange
        _mockService
            .Setup(s => s.AggregateTransactionsFromSourcesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AggregateTransactions(CancellationToken.None);

        // Assert
        result.Should().BeOfType<AcceptedResult>();
        _mockService.Verify(s => s.AggregateTransactionsFromSourcesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTransactionsByCategory_ShouldReturnOk_WithFilteredTransactions()
    {
        // Arrange
        var customerId = "CUST001";
        var category = TransactionCategory.Groceries;
        var mockTransactions = new List<Transaction>
        {
            CreateMockTransaction(Guid.NewGuid(), customerId, 50m, category: category),
            CreateMockTransaction(Guid.NewGuid(), customerId, 75m, category: category)
        };

        _mockService
            .Setup(s => s.GetTransactionsByCategoryAsync(customerId, category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransactions);

        // Act
        var result = await _controller.GetTransactionsByCategory(customerId, category, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;

        var response = okResult!.Value as IEnumerable<TransactionResponse>;
        response.Should().NotBeNull();
        response.Should().HaveCount(2);
        response.Should().AllSatisfy(t => t.Category.Should().Be(category.ToString()));
    }

    #region Helper Methods

    private static Transaction CreateMockTransaction(
        Guid id,
        string customerId,
        decimal amount,
        DateTime? transactionDate = null,
        TransactionCategory category = TransactionCategory.Groceries)
    {
        return new Transaction
        {
            Id = id,
            CustomerId = customerId,
            AccountId = "ACC001",
            Amount = amount,
            Currency = "USD",
            TransactionDate = transactionDate ?? DateTime.UtcNow,
            Type = TransactionType.Debit,
            Category = category,
            Description = "Test transaction",
            MerchantName = "Test Merchant",
            Status = TransactionStatus.Completed,
            SourceSystem = "TestSystem",
            Reference = $"REF-{id}",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
