# Use the official .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY TransactionAggregatorAPI.API.slnx ./
COPY TransactionAggregatorAPI.API/TransactionAggregatorAPI.API.csproj ./TransactionAggregatorAPI.API/
COPY TransactionAggregatorAPI.Domain/TransactionAggregatorAPI.Domain.csproj ./TransactionAggregatorAPI.Domain/
COPY TransactionAggregatorAPI.DataAccess/TransactionAggregatorAPI.DataAccess.csproj ./TransactionAggregatorAPI.DataAccess/
COPY TransactionAggregatorAPI.Tests/TransactionAggregatorAPI.Tests.csproj ./TransactionAggregatorAPI.Tests/

# Restore dependencies
RUN dotnet restore "TransactionAggregatorAPI.API/TransactionAggregatorAPI.API.csproj"
RUN dotnet restore "TransactionAggregatorAPI.Domain/TransactionAggregatorAPI.Domain.csproj"
RUN dotnet restore "TransactionAggregatorAPI.DataAccess/TransactionAggregatorAPI.DataAccess.csproj"
RUN dotnet restore "TransactionAggregatorAPI.Tests/TransactionAggregatorAPI.Tests.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
WORKDIR TransactionAggregatorAPI.API
RUN dotnet build -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Use the official .NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Expose the application port
EXPOSE 8080
EXPOSE 8081

# Copy the published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "TransactionAggregatorAPI.API.dll"]
