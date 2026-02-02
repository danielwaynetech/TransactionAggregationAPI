namespace TransactionAggregatorAPI.Domain.Exceptions;

public class TransactionAggregatorException : Exception
{
    public TransactionAggregatorException(string message) : base(message)
    {
    }

    public TransactionAggregatorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public class TransactionNotFoundException : TransactionAggregatorException
{
    public Guid TransactionId { get; }

    public TransactionNotFoundException(Guid transactionId)
        : base($"Transaction with ID '{transactionId}' was not found.")
    {
        TransactionId = transactionId;
    }
}

public class InvalidCustomerIdException : TransactionAggregatorException
{
    public string CustomerId { get; }

    public InvalidCustomerIdException(string customerId)
        : base($"Customer ID '{customerId}' is invalid or empty.")
    {
        CustomerId = customerId;
    }
}

public class InvalidAccountIdException : TransactionAggregatorException
{
    public string AccountId { get; }

    public InvalidAccountIdException(string accountId)
        : base($"Account ID '{accountId}' is invalid or empty.")
    {
        AccountId = accountId;
    }
}

public class InvalidDateRangeException : TransactionAggregatorException
{
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }

    public InvalidDateRangeException(DateTime startDate, DateTime endDate)
        : base($"Invalid date range: Start date ({startDate:yyyy-MM-dd}) must be before end date ({endDate:yyyy-MM-dd}).")
    {
        StartDate = startDate;
        EndDate = endDate;
    }
}

public class DataSourceException : TransactionAggregatorException
{
    public string SourceName { get; }

    public DataSourceException(string sourceName, string message)
        : base($"Error fetching data from source '{sourceName}': {message}")
    {
        SourceName = sourceName;
    }

    public DataSourceException(string sourceName, string message, Exception innerException)
        : base($"Error fetching data from source '{sourceName}': {message}", innerException)
    {
        SourceName = sourceName;
    }
}

public class InvalidTransactionDataException : TransactionAggregatorException
{
    public InvalidTransactionDataException(string message)
        : base($"Invalid transaction data: {message}")
    {
    }
}
