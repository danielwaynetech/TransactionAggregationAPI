using AspNetCoreRateLimit;
using TransactionAggregatorAPI.API.Extensions;
using TransactionAggregatorAPI.DataAccess;
using TransactionAggregatorAPI.DataAccess.Repositories;
using TransactionAggregatorAPI.Domain.Contracts;
using TransactionAggregatorAPI.Domain.Services;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Polly.Registry;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Data.Common;
using PolicyRegistry = TransactionAggregatorAPI.API.Extensions.PolicyRegistry;
using TransactionAggregatorAPI.DataAccess.Mapper;
using Microsoft.Extensions.DependencyInjection;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .WriteTo.Console(new JsonFormatter())
    .WriteTo.File(
        new JsonFormatter(),
        "logs/financial-aggregator-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    Log.Information("Starting Financial Aggregator API");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers()
        .AddOData(options => options
            .Select()
            .Filter()
            .OrderBy()
            .SetMaxTop(100)
            .Count()
            .Expand());

    // Configure Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title = "Transaction Aggregator API",
            Version = "v1.0.0",
            Description = "Production-grade API for aggregating transaction data with authentication, caching, resilience, and audit logging"
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }

        // Add API Key authentication to Swagger
        c.AddSecurityDefinition("ApiKey", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "API Key authentication. Add your API key in the X-API-Key header.",
            Name = "X-API-Key",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "ApiKeyScheme"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "ApiKey"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Configure Database - PostgreSQL
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Host=postgres;Database=financial_aggregator;Username=postgres;Password=postgres;Port=5432";

    builder.Services.AddDbContext<TransactionDbContext>(options =>
        options.UseNpgsql(connectionString, x =>
                x.MigrationsAssembly("TransactionAggregatorAPI.API"))
            .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
            .EnableDetailedErrors(builder.Environment.IsDevelopment()));

    // Configure Redis Cache
    var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";

    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "FinancialAggregator_";
    });

    // Register cache service
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();

    // Register audit service
    builder.Services.AddScoped<IAuditService, AuditService>();

    // Register policy registry
    builder.Services.AddSingleton<PolicyRegistry>();

    // Configure Rate Limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(options =>
    {
        options.EnableEndpointRateLimiting = true;
        options.StackBlockedRequests = false;
        options.HttpStatusCode = 429;
        options.RealIpHeader = "X-Real-IP";
        options.ClientIdHeader = "X-ClientId";
        options.GeneralRules = new List<RateLimitRule>
        {
            new RateLimitRule
            {
                Endpoint = "*",
                Period = "1m",
                Limit = 100
            },
            new RateLimitRule
            {
                Endpoint = "*",
                Period = "1h",
                Limit = 1000
            },
            new RateLimitRule
            {
                Endpoint = "POST:*/aggregate",
                Period = "1h",
                Limit = 10
            }
        };
    });

    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

    // Register AutoMapper from Data layer
    builder.Services.AddAutoMapper(typeof(MapperProfile).Assembly);

    // Register repositories
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

    // Register services
    builder.Services.AddScoped<ITransactionService, TransactionService>();

    // Register data sources
    builder.Services.AddScoped<IDataSourceService, BankADataSource>();
    builder.Services.AddScoped<IDataSourceService, BankBDataSource>();
    builder.Services.AddScoped<IDataSourceService, CreditCardDataSource>();

    // Register exception handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // Add Health Checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "PostgreSQL", timeout: TimeSpan.FromSeconds(5))
        .AddRedis(redisConnection, name: "Redis", timeout: TimeSpan.FromSeconds(5));

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Aggregator API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root
        c.DocumentTitle = "Financial Aggregator API";
    });

    // Use Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString());
        };
    });

    // Use exception handler
    app.UseExceptionHandler();

    // Use API key authentication (before rate limiting)
    app.UseApiKeyAuthentication();

    // Use rate limiting
    app.UseIpRateLimiting();

    // Ensure database is created and migrations applied
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<TransactionDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database migration failed. Attempting to create database...");
            try
            {
                await dbContext.Database.EnsureCreatedAsync();
                logger.LogInformation("Database created successfully");
            }
            catch (Exception createEx)
            {
                logger.LogError(createEx, "Failed to create database. Application may not function correctly.");
            }
        }
    }

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("Financial Aggregator API started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public for testing
public partial class Program { }
