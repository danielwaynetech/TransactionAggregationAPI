# Sample API Requests

This document provides example API requests you can use to test the Transaction Aggregator API.

## Base URL
- Local: `http://localhost:5000` or `https://localhost:5001`
- Docker: `http://localhost:8080`

## 1. Aggregate Mock Data

First, populate the database with mock transactions from all data sources:

```bash
curl -X POST http://localhost:8080/api/transactions/aggregate
```

Expected Response: `202 Accepted`
```json
{
  "message": "Transaction aggregation completed successfully"
}
```

## 2. Get All Transactions for Customer CUST001

```bash
curl http://localhost:8080/api/transactions/customer/CUST001
```

Expected Response: `200 OK`
```json
[
  {
    "id": "uuid-here",
    "customerId": "CUST001",
    "accountId": "ACC-BANKA-001",
    "amount": 1250.50,
    "currency": "USD",
    "transactionDate": "2024-01-26T00:00:00Z",
    "type": "Credit",
    "category": "Income",
    "description": "Salary Deposit",
    "merchantName": "ACME Corp",
    "status": "Completed",
    "sourceSystem": "BankA",
    "reference": "REF-BANKA-001",
    "createdAt": "2024-01-31T10:00:00Z",
    "updatedAt": null
  }
]
```

## 3. Get Transaction Summary

Get a summary of all transactions for a customer in a date range:

```bash
curl "http://localhost:8080/api/transactions/customer/CUST001/summary?startDate=2024-01-01T00:00:00Z&endDate=2024-12-31T23:59:59Z"
```

Expected Response: `200 OK`
```json
{
  "customerId": "CUST001",
  "startDate": "2024-01-01T00:00:00Z",
  "endDate": "2024-12-31T23:59:59Z",
  "totalIncome": 1250.50,
  "totalExpenses": 780.49,
  "netAmount": 470.01,
  "transactionCount": 8,
  "categoryBreakdown": [
    {
      "category": "Income",
      "totalAmount": 1250.50,
      "transactionCount": 1,
      "percentage": 38.52
    },
    {
      "category": "Shopping",
      "totalAmount": 299.99,
      "transactionCount": 1,
      "percentage": 9.24
    }
  ]
}
```

## 4. Get Customer Summary

```bash
curl http://localhost:8080/api/transactions/customer/CUST001/customer-summary
```

Expected Response: `200 OK`
```json
{
  "customerId": "CUST001",
  "totalAccounts": 3,
  "totalBalance": 470.01,
  "totalTransactions": 8,
  "lastTransactionDate": "2024-01-30T00:00:00Z"
}
```

## 5. Get Transactions by Category

Get all grocery transactions for a customer:

```bash
curl http://localhost:8080/api/transactions/customer/CUST001/category/Groceries
```

Expected Response: `200 OK`
```json
[
  {
    "id": "uuid-here",
    "customerId": "CUST001",
    "accountId": "ACC-BANKA-001",
    "amount": 45.99,
    "currency": "USD",
    "transactionDate": "2024-01-27T00:00:00Z",
    "type": "Debit",
    "category": "Groceries",
    "description": "Grocery Shopping",
    "merchantName": "Whole Foods Market",
    "status": "Completed",
    "sourceSystem": "BankA",
    "reference": "REF-BANKA-002",
    "createdAt": "2024-01-31T10:00:00Z",
    "updatedAt": null
  }
]
```

## 6. Get Transactions by Date Range

```bash
curl "http://localhost:8080/api/transactions/customer/CUST001/date-range?startDate=2024-01-25T00:00:00Z&endDate=2024-01-28T23:59:59Z"
```

## 7. Create a New Transaction

```bash
curl -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST001",
    "accountId": "ACC001",
    "amount": 75.50,
    "currency": "USD",
    "transactionDate": "2024-01-31T14:30:00Z",
    "type": "Debit",
    "category": "Dining",
    "description": "Lunch at downtown restaurant",
    "merchantName": "The Blue Cafe",
    "status": "Completed",
    "sourceSystem": "ManualEntry",
    "reference": "MANUAL-001"
  }'
```

Expected Response: `201 Created`
```json
{
  "id": "newly-created-uuid",
  "customerId": "CUST001",
  "accountId": "ACC001",
  "amount": 75.50,
  "currency": "USD",
  "transactionDate": "2024-01-31T14:30:00Z",
  "type": "Debit",
  "category": "Dining",
  "description": "Lunch at downtown restaurant",
  "merchantName": "The Blue Cafe",
  "status": "Completed",
  "sourceSystem": "ManualEntry",
  "reference": "MANUAL-001",
  "createdAt": "2024-01-31T15:00:00Z",
  "updatedAt": null
}
```

## 8. Get Specific Transaction by ID

After creating or getting a transaction ID:

```bash
curl http://localhost:8080/api/transactions/{transaction-id}
```

Replace `{transaction-id}` with an actual GUID from previous responses.

## Available Transaction Types
- `Debit` - Money out
- `Credit` - Money in

## Available Transaction Categories
- `Unknown`
- `Groceries`
- `Dining`
- `Transportation`
- `Entertainment`
- `Shopping`
- `Utilities`
- `Healthcare`
- `Education`
- `Travel`
- `Income`
- `Transfer`
- `Investment`
- `Insurance`
- `Housing`
- `PersonalCare`
- `Professional`
- `Other`

## Available Transaction Statuses
- `Pending`
- `Completed`
- `Failed`
- `Cancelled`

## Testing the API with Postman

1. Import the API into Postman using the Swagger URL:
   - `http://localhost:8080/swagger/v1/swagger.json`

2. Or create a new collection with the endpoints above

3. Set your environment variables:
   - `BASE_URL`: `http://localhost:8080`

## Quick Test Sequence

Run these commands in order to test the full workflow:

```bash
# 1. Aggregate mock data
curl -X POST http://localhost:8080/api/transactions/aggregate

# 2. View all customers
curl http://localhost:8080/api/transactions/customer/CUST001
curl http://localhost:8080/api/transactions/customer/CUST002
curl http://localhost:8080/api/transactions/customer/CUST003

# 3. Get summaries
curl "http://localhost:8080/api/transactions/customer/CUST001/summary?startDate=2024-01-01&endDate=2024-12-31"
curl http://localhost:8080/api/transactions/customer/CUST001/customer-summary

# 4. Create a new transaction
curl -X POST http://localhost:8080/api/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "CUST001",
    "accountId": "ACC999",
    "amount": 250.00,
    "currency": "USD",
    "transactionDate": "2024-01-31T16:00:00Z",
    "type": "Credit",
    "category": "Income",
    "description": "Freelance Payment",
    "merchantName": "Client ABC",
    "status": "Completed",
    "sourceSystem": "API"
  }'

# 5. View updated summary
curl "http://localhost:8080/api/transactions/customer/CUST001/summary?startDate=2024-01-01&endDate=2024-12-31"
```

## Error Responses

### 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Customer ID is required"
}
```

### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404
}
```

### 500 Internal Server Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```
