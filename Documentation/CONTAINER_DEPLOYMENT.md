# Running Transaction Aggregator API with Docker Containers

This guide explains how to run the Transaction Aggregator API system locally using Docker containers.

## Prerequisites

- **Docker Desktop** installed ([Download here](https://www.docker.com/products/docker-desktop/))
- **Docker Compose** (included with Docker Desktop)
- At least 4GB of available RAM
- Ports 5432 (PostgreSQL), 6379 (Redis), and 8080 (API) available

## Architecture Overview

The containerized solution includes three services:
1. **PostgreSQL** - Database (port 5432)
2. **Redis** - Cache (port 6379)
3. **API** - .NET Application (port 8080)

All services communicate on a Docker network called `financial-network`.

## Quick Start

### Option 1: Using Docker Compose (Recommended)

This is the easiest way to run everything:

```bash
# 1. Navigate to the project directory
cd FinancialAggregator

# 2. Build and start all containers
docker-compose up -d

# 3. View logs
docker-compose logs -f

# 4. Check status
docker-compose ps
```

**Access the application:**
- Swagger UI: http://localhost:8080
- PostgreSQL: localhost:5432
- Redis: localhost:6379

### Option 2: Using Docker CLI (Manual)

If you prefer manual control:

```bash
# 1. Create a Docker network
docker network create financial-network

# 2. Start PostgreSQL
docker run -d \
  --name financial-aggregator-db \
  --network financial-network \
  -e POSTGRES_DB=financial_aggregator \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  -v postgres-data:/var/lib/postgresql/data \
  postgres:16-alpine

# 3. Start Redis
docker run -d \
  --name financial-aggregator-redis \
  --network financial-network \
  -p 6379:6379 \
  -v redis-data:/data \
  redis:7-alpine

# 4. Build the API image
docker build -t financial-aggregator:latest .

# 5. Run the API container
docker run -d \
  --name financial-aggregator-api \
  --network financial-network \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  -e ConnectionStrings__DefaultConnection="Host=financial-aggregator-db;Database=financial_aggregator;Username=postgres;Password=postgres;Port=5432" \
  -e ConnectionStrings__Redis="financial-aggregator-redis:6379" \
  financial-aggregator:latest
```


## Verifying the Setup

### 1. Check All Containers Are Running

```bash
docker-compose ps
```

Expected output:
```
NAME                           STATUS    PORTS
financial-aggregator-api       Up        0.0.0.0:8080->8080/tcp
financial-aggregator-db        Up        0.0.0.0:5432->5432/tcp
financial-aggregator-redis     Up        0.0.0.0:6379->6379/tcp
```

### 2. Check Container Logs

```bash
# API logs
docker-compose logs financial-aggregator-api

# PostgreSQL logs
docker-compose logs postgres

# Redis logs
docker-compose logs redis

# All logs
docker-compose logs -f
```

### 3. Test Database Connection

```bash
# Connect to PostgreSQL
docker exec -it financial-aggregator-db psql -U postgres -d financial_aggregator

# List tables
\dt

# Exit
\q
```

### 4. Test Redis Connection

```bash
# Connect to Redis
docker exec -it financial-aggregator-redis redis-cli

# Test
PING
# Should respond: PONG

# Check keys
KEYS *

# Exit
exit
```

### 5. Test API

See API_SAMPLES.md in the Documentation folder.


## Summary

**To run locally:**
1. Install Docker Desktop
2. Run `docker-compose up -d`
3. Access Swagger at http://localhost:8080
4. Connect to PostgreSQL at localhost:5432
5. Connect to Redis at localhost:6379