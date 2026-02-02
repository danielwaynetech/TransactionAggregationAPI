# Transaction Aggregator API

A production-grade .NET 8 API for aggregating financial transaction data with enterprise features including API key authentication, caching, resilience patterns, audit logging, and comprehensive monitoring.

## 🎯 Production Features

### ✅ Security
- **API Key Authentication** - Required for all endpoints (except health checks)
- **Rate Limiting** - 100 requests/minute, 1000 requests/hour
- **Input Validation** - Data annotations on all models/entities
- **Soft Deletes** - No physical data deletion, full audit trail

 Development
- Dev, Test and Admin api keys are specified in the appsettings.json file.

 Production
- Use strong, random API keys (min 32 characters)
- Store keys in Azure Key Vault / AWS Secrets Manager
- Enable HTTPS only
- Implement additional authentication (JWT, OAuth2)
- Set stricter rate limits
- Enable CORS selectively


### ✅ Resilience & Reliability
- **Polly Retry Policy** - 3 attempts with exponential backoff (2s, 4s, 8s)
- **Circuit Breaker** - Opens after 5 consecutive failures, 30s break
- **Timeout Policies** - 30s for DB, 10s for cache, 60s for data sources
- **Global Exception Handling** - RFC 7807 Problem Details format

### ✅ Performance
- **Redis Caching** - Distributed cache with 5-15 minute TTL
- **Database Indexing** - 6 indexes on Transactions table
- **Async/Await** - Non-blocking I/O throughout
- **Connection Pooling** - EF Core and Npgsql optimization

### ✅ Monitoring & Observability
- **Health Checks** - PostgreSQL, Redis, and application health
- **Metrics Endpoint** - System and application metrics
- **Structured Logging** - Serilog with enrichers (environment, thread, timestamp)
- **Audit Logging** - Complete audit trail for all data modifications

### ✅ Data Management
- **Soft Delete** - IsDeleted flag instead of physical deletion
- **Audit Trail** - Tracks who, when, what changed with before/after values
- **OData Queries** - $filter, $orderby, $top, $skip, $count support

### ✅ Developer Experience
- **XML Documentation** - All endpoints and models documented
- **Swagger UI** - Interactive API documentation
- **.http File** - Ready-to-use endpoint tests
- **Docker Compose** - One-command deployment

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) OR
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Ports: 5432 (PostgreSQL), 6379 (Redis), 8080 (API)

## 🚀 Start

See CONTAINER_DEPLOYMENT.md in the Documentation folder.

## 🔑 API Authentication

All endpoints require an API key (except `/health/*`).


## 📊 Database Schema

### Tables

**Transactions**
- Primary entity with soft delete support
- 16 columns including audit fields
- 6 indexes for query optimization
- Global query filter: `WHERE IsDeleted = false`

**AuditLogs**
- Tracks all data modifications
- Stores before/after values in JSON
- Records who, when, and from where

See Database diagram (.svg) in the Documentation folder.

## 🔍 API Endpoints

### Health & Monitoring (No Auth Required)
```http
GET  /health              # Detailed health check
GET  /health/live         # Liveness probe (K8s)
GET  /health/ready        # Readiness probe (K8s)
GET  /health/metrics      # System metrics
```

### Transactions (Auth Required)
```http
POST /api/transactions/aggregate                              # Load from data sources
GET  /api/transactions/{id}                                   # Get by ID (cached 10min)
GET  /api/transactions/customer/{customerId}                  # Get all (cached 5min, OData)
GET  /api/transactions/customer/{customerId}/date-range       # Filter by date (OData)
GET  /api/transactions/customer/{customerId}/category/{cat}   # Filter by category (OData)
GET  /api/transactions/customer/{customerId}/summary          # Transaction summary (cached 15min)
GET  /api/transactions/customer/{customerId}/customer-summary # Customer overview (cached 10min)
POST /api/transactions                                        # Create transaction
DELETE /api/transactions/{id}                                 # Soft delete (IsDeleted=true)
GET  /api/transactions/{id}/audit                            # Get audit history
```

### OData Query Examples
```http
# Filter by amount > 100
GET /api/transactions/customer/CUST001?$filter=amount gt 100
X-API-Key: dev-key-12345

# Sort by date descending
GET /api/transactions/customer/CUST001?$orderby=transactionDate desc
X-API-Key: dev-key-12345

# Pagination (top 20, skip first 40)
GET /api/transactions/customer/CUST001?$top=20&$skip=40
X-API-Key: dev-key-12345

# Combined query with count
GET /api/transactions/customer/CUST001?$filter=amount gt 50&$orderby=amount desc&$top=10&$count=true
X-API-Key: dev-key-12345
```

## 🧪 Testing the API

### Using the .http File

1. Open `FinancialAggregator.http` in Visual Studio or VS Code with REST Client extension
2. Click "Send Request" on any endpoint
3. API key is pre-configured in variables

See API_SAMPLES.md for further API request samples with expected responses.

### Structured Logging

Logs include:
- **Timestamp** - When event occurred
- **Level** - Debug, Information, Warning, Error
- **Environment** - Development, Production
- **Thread** - Thread ID
- **SourceContext** - Logger name
- **Message** - Log message with structured properties

Example:
```json
{
  "Timestamp": "2024-02-01T10:15:23.1234567Z",
  "Level": "Information",
  "MessageTemplate": "Authenticated request. Path: {Path}, IP: {IP}",
  "Properties": {
    "Path": "/api/transactions/customer/CUST001",
    "IP": "172.18.0.1",
    "Environment": "Development",
    "ThreadId": 42
  }
}
```

## 🔧 Configuration

### appsettings.json

The appsettings.json specifies:
- API keys
- PostgreSQL connection string
- Redis connection string
- Logging


## 🏗️ Architecture

See Transaction Aggregator API Architecture (.svg) in the Documentation folder.

**Key Layers:**
1. **API Gateway** - Authentication, rate limiting
2. **API Layer** - Controllers, caching, resilience
3. **Domain Layer** - Business logic, validation
4. **Data Layer** - Repository, EF Core, AutoMapper
5. **Infrastructure** - PostgreSQL, Redis

**Resilience Flow:**
```
Request → Timeout (30s) → Retry (3x) → Circuit Breaker → Service
```

## 💾 Database

See Database diagram (.svg) in the documentation folder.

**Tables:**
- `Transactions` - 16 columns, 6 indexes, soft delete enabled
- `AuditLogs` - Complete audit trail with JSON snapshots

**Key Features:**
- Soft delete with global query filter
- Composite indexes for performance
- Audit logging for compliance
- Data annotations for validation

NOTE: While the data access repository contains methods to update and soft-delete transactions, these are not exposed to the API.
This is because the purpose of this API is to aggregate transactions.

## 🧪 Running Tests

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run specific test class
dotnet test --filter TransactionServiceTests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

**Test Coverage:**
- Unit tests for all services
- Controller tests with Moq
- Exception handling tests
- Resilience policy tests
- Soft delete tests
- Audit logging tests
