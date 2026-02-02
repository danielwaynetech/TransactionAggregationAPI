using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Data.Common;
using TransactionAggregatorAPI.Domain.Contracts;
using TransactionAggregatorAPI.Domain.Exceptions;
using TransactionAggregatorAPI.Domain.Models;
using TransactionAggregatorAPI.Domain.Services;
using Xunit;

namespace FinancialAggregator.Tests.Services;

public class TransactionServiceExceptionTests
{
    private readonly Mock<ITransactionRepository> _mockRepository;
    private readonly Mock<ILogger<TransactionService>> _mockLogger;
    private readonly TransactionService _service;

    public TransactionServiceExceptionTests()
    {
        _mockRepository = new Mock<ITransactionRepository>();
        _mockLogger = new Mock<ILogger<TransactionService>>();

        var mockDataSource = new Mock<IDataSourceService>();
        mockDataSource.Setup(s => s.SourceName).Returns("TestSource");

        var dataSources = new List<IDataSourceService> { mockDataSource.Object };
        _service = new TransactionService(_mockRepository.Object, dataSources, _mockLogger.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
    [InlineData(null)]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
    public async Task GetCustomerTransactionsAsync_ShouldThrowInvalidCustomerIdException_WhenCustomerIdIsInvalid(string customerId)
    {
        // Act
        Func<Task> act = async () => await _service.GetCustomerTransactionsAsync(customerId);

        // Assert
        await act.Should().ThrowAsync<InvalidCustomerIdException>()
            .WithMessage("*Customer ID*invalid*");
    }

    [Fact]
    public async Task GetTransactionsByDateRangeAsync_ShouldThrowInvalidDateRangeException_WhenStartDateAfterEndDate()
    {
        // Arrange
        var customerId = "CUST001";
        var startDate = new DateTime(2024, 1, 15);
        var endDate = new DateTime(2024, 1, 10);

        // Act
        Func<Task> act = async () => await _service.GetTransactionsByDateRangeAsync(customerId, startDate, endDate);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidDateRangeException>();
        exception.Which.StartDate.Should().Be(startDate);
        exception.Which.EndDate.Should().Be(endDate);
    }

    [Fact]
    public async Task CreateTransactionAsync_ShouldThrowInvalidAccountIdException_WhenAccountIdIsEmpty()
    {
        // Arrange
        var transaction = new Transaction
        {
            CustomerId = "CUST001",
            AccountId = "",
            Amount = 100m
        };

        // Act
        Func<Task> act = async () => await _service.CreateTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<InvalidAccountIdException>()
            .WithMessage("*Account ID*invalid*");
    }

    [Fact]
    public async Task CreateTransactionAsync_ShouldThrowInvalidTransactionDataException_WhenAmountIsNegative()
    {
        // Arrange
        var transaction = new Transaction
        {
            CustomerId = "CUST001",
            AccountId = "ACC001",
            Amount = -50m
        };

        // Act
        Func<Task> act = async () => await _service.CreateTransactionAsync(transaction);

        // Assert
        await act.Should().ThrowAsync<InvalidTransactionDataException>()
            .WithMessage("*Amount cannot be negative*");
    }

    [Fact]
    public async Task AggregateTransactionsFromSourcesAsync_ShouldThrowDataSourceException_WhenSourceFails()
    {
        // Arrange
        var mockDataSource = new Mock<IDataSourceService>();
        mockDataSource.Setup(s => s.SourceName).Returns("FailingSource");
        mockDataSource
            .Setup(s => s.FetchTransactionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection timeout"));

        var service = new TransactionService(
            _mockRepository.Object,
            new[] { mockDataSource.Object },
            _mockLogger.Object);

        // Act
        Func<Task> act = async () => await service.AggregateTransactionsFromSourcesAsync();

        // Assert
        var exception = await act.Should().ThrowAsync<DataSourceException>();
        exception.Which.SourceName.Should().Be("FailingSource");
        exception.Which.Message.Should().Contain("FailingSource");
    }
}
