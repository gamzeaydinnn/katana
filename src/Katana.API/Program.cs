using Katana.API.Middleware;
using Katana.Data.Configuration;
using Katana.Business.Services;
using Katana.Business.Interfaces;
using Katana.Data.Context;
using Katana.Data.Repositories;
using Katana.Business.Jobs;
using Katana.Infrastructure.Logging;
using Katana.Infrastructure.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System.Text;
using Katana.Infrastructure.APIClients;
using Katana.Business.UseCases.Sync;
using Katana.Infrastructure.Notifications;
using Katana.Core.Interfaces;
using Katana.Core.Entities;


var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Logging (Serilog)
// -----------------------------
builder.Host.UseSerilogConfiguration();

// -----------------------------
// Services
// -----------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Frontend ile tutarlı camelCase JSON ve case-insensitive model binding
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();

// -----------------------------
// Swagger Configuration
// -----------------------------
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Katana-Luca Integration API",
        Version = "v1",
        Description = "API for managing integration between Katana MRP/ERP and Luca Accounting systems"
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the endpoints. X-API-Key: Your_API_Key",
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// -----------------------------
// Database
// -----------------------------
builder.Services.AddDbContext<IntegrationDbContext>(options =>
{
    var env = builder.Environment;
    var sqliteConnection = builder.Configuration.GetConnectionString("DefaultConnection");
    var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServerConnection");

    // Enforce SQL Server usage. Do NOT fall back to SQLite — migrations and runtime must target SQL Server.
    if (!string.IsNullOrWhiteSpace(sqlServerConnection))
    {
        // Enable basic transient fault handling for SQL Server connections
        options.UseSqlServer(sqlServerConnection, sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
        return;
    }

    // If we reach here, SQL Server connection is not configured — fail fast so developer provides the intended DB.
    throw new InvalidOperationException("SqlServerConnection is not configured. This application requires SQL Server as the database. Set 'ConnectionStrings:SqlServerConnection' to a valid SQL Server connection string.");
});

// -----------------------------
// Configuration Bindings
// -----------------------------
builder.Services.Configure<KatanaApiSettings>(builder.Configuration.GetSection("KatanaApi"));
builder.Services.Configure<LucaApiSettings>(builder.Configuration.GetSection("LucaApi"));

builder.Services.AddAuthorization();

// -----------------------------
// HTTP Clients
// -----------------------------
builder.Services.AddHttpClient<IKatanaService, KatanaService>((serviceProvider, client) =>
{
    var katanaSettings = serviceProvider.GetRequiredService<IOptions<KatanaApiSettings>>().Value;
    client.BaseAddress = new Uri(katanaSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(katanaSettings.TimeoutSeconds);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", katanaSettings.ApiKey?.Trim());
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient<ILucaService, LucaService>((serviceProvider, client) =>
{
    var lucaSettings = serviceProvider.GetRequiredService<IOptions<LucaApiSettings>>().Value;
    client.BaseAddress = new Uri(lucaSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(lucaSettings.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrEmpty(lucaSettings.ApiKey) && !lucaSettings.UseTokenAuth)
    {
        client.DefaultRequestHeaders.Add("X-API-Key", lucaSettings.ApiKey);
    }
});

// Server-side cookie jar for Luca session handling
builder.Services.AddSingleton<ILucaCookieJarStore, LucaCookieJarStore>();

// -----------------------------
// Repository + UnitOfWork
// -----------------------------
builder.Services.AddScoped(typeof(Katana.Core.Interfaces.IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IRepository<Category>, CategoryRepository>();
//builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// -----------------------------
// Business Services
// -----------------------------
builder.Services.AddScoped<IExtractorService, ExtractorService>();
builder.Services.AddScoped<ITransformerService, TransformerService>();
builder.Services.AddScoped<ILoaderService, LoaderService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IIntegrationService>(sp => (IIntegrationService)sp.GetRequiredService<ISyncService>());
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IErrorHandler, ErrorHandlerService>();

// Logging Service
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// Notification sistemi
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
builder.Services.AddScoped<NotificationService>(); // Business katmanı

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AdminService>();

// -----------------------------
// JWT Authentication
// -----------------------------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// -----------------------------
// CORS
// -----------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// -----------------------------
// Health Checks
// -----------------------------
builder.Services.AddHealthChecks().AddDbContextCheck<IntegrationDbContext>();

// -----------------------------
// Background Services (Worker + Quartz) - Disabled (requires Luca API)
// -----------------------------
//builder.Services.AddHostedService<Katana.Infrastructure.Workers.SyncWorkerService>();

builder.Services.AddQuartz(q =>
{
    // Stock sync job - every 6 hours
    var stockJobKey = new JobKey("StockSyncJob");
    q.AddJob<SyncJob>(opts => opts.WithIdentity(stockJobKey).UsingJobData("SyncType", "STOCK"));
    q.AddTrigger(opts => opts.ForJob(stockJobKey).WithIdentity("StockSyncTrigger").WithCronSchedule("0 0 */6 * * ?"));

    // Invoice sync job - every 4 hours
    var invoiceJobKey = new JobKey("InvoiceSyncJob");
    q.AddJob<SyncJob>(opts => opts.WithIdentity(invoiceJobKey).UsingJobData("SyncType", "INVOICE"));
    q.AddTrigger(opts => opts.ForJob(invoiceJobKey).WithIdentity("InvoiceSyncTrigger").WithCronSchedule("0 0 */4 * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

// Pending DB write queue + background flusher
// Register the Core implementation so the IntegrationDbContext and the background worker
// (which depend on Katana.Core.Services.PendingDbWriteQueue) receive the same instance.
builder.Services.AddSingleton<Katana.Core.Services.PendingDbWriteQueue>();
builder.Services.AddHostedService<Katana.Infrastructure.Workers.RetryPendingDbWritesService>();

// -----------------------------
// Build & Run
// -----------------------------
var app = builder.Build();

// Swagger her zaman açık (Development ve Production)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Katana-Luca Integration API v1");
    c.RoutePrefix = string.Empty; // Swagger UI ana dizinde
});

// Routing (CORS öncesi)
app.UseRouting();

// CORS (UseRouting ile UseAuthentication/Authorization arasında olmalı)
app.UseCors("AllowFrontend");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Custom Middlewares (Authentication'dan SONRA)
app.UseMiddleware<ErrorHandlingMiddleware>();

// Endpoints
app.MapControllers();
app.MapHealthChecks("/health");

// Geliştirme ortamında veritabanını otomatik oluştur (SQLite için pratik)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
    try
    {
        db.Database.EnsureCreated();
    }
    catch
    {
        // Dev kolaylığı: oluşturma başarısız olsa bile uygulama devam etsin
    }
}
app.Run();
