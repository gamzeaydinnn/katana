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
using System.Text.RegularExpressions;


var builder = WebApplication.CreateBuilder(args);

// Prefer URLs from env/config; otherwise use a non-conflicting default port (5055)
var envUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var configuredUrls = builder.Configuration["Urls"];
if (string.IsNullOrWhiteSpace(envUrls) && string.IsNullOrWhiteSpace(configuredUrls))
{
    // Pick the first available port among a small set to avoid collisions
    int[] preferred =
    {
        builder.Configuration.GetValue<int?>("Server:Port") ?? 5055,
        5056, 5057, 5058, 5059
    };

    int chosen = preferred[0];
    foreach (var p in preferred)
    {
        try
        {
            var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, p);
            l.Start();
            l.Stop();
            chosen = p;
            break;
        }
        catch { /* in use, try next */ }
    }

    builder.WebHost.ConfigureKestrel(options => { options.ListenLocalhost(chosen); });
    Console.WriteLine($"Kestrel chosen port: {chosen}");
}

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

    if (env.IsDevelopment())
    {
        // Enforce SQL Server usage in development to match production-like behavior.
        // Do NOT fall back to SQLite automatically — require an explicit SqlServerConnection.
        if (!string.IsNullOrWhiteSpace(sqlServerConnection))
        {
            options.UseSqlServer(sqlServerConnection, sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
            return;
        }

        // If a developer intentionally wants a lightweight local DB, they must explicitly
        // set ConnectionStrings:DefaultConnection to a sqlite connection string and opt-in
        // by setting the environment variable ALLOW_SQLITE_FALLBACK=true. This avoids
        // accidental local SQLite usage when the team expects SQL Server.
        var allowSqlite = string.Equals(Environment.GetEnvironmentVariable("ALLOW_SQLITE_FALLBACK"), "true", StringComparison.OrdinalIgnoreCase);
        if (allowSqlite && !string.IsNullOrWhiteSpace(sqliteConnection))
        {
            options.UseSqlite(sqliteConnection);
            return;
        }

        throw new InvalidOperationException("SqlServerConnection is required for development. Set 'ConnectionStrings:SqlServerConnection' or enable explicit SQLite fallback by setting environment variable ALLOW_SQLITE_FALLBACK=true and providing ConnectionStrings:DefaultConnection.");
    }

    // Production/Staging: require SQL Server, do not fall back silently
    if (!string.IsNullOrWhiteSpace(sqlServerConnection))
    {
        options.UseSqlServer(sqlServerConnection, sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
        return;
    }

    throw new InvalidOperationException("SqlServerConnection is not configured. Set 'ConnectionStrings:SqlServerConnection' for non-development environments.");
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
// Register KatanaService as a typed HttpClient implementation and map the interface to the concrete
// implementation via DI. This ensures the concrete `KatanaService` is created through the
// IServiceProvider (ActivatorUtilities) so additional services like IMemoryCache are injected
// correctly at runtime.
builder.Services.AddHttpClient<KatanaService>((serviceProvider, client) =>
{
    var katanaSettings = serviceProvider.GetRequiredService<IOptions<KatanaApiSettings>>().Value;
    client.BaseAddress = new Uri(katanaSettings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(katanaSettings.TimeoutSeconds);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", katanaSettings.ApiKey?.Trim());
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
})
// reasonable handler lifetime
.SetHandlerLifetime(TimeSpan.FromMinutes(5));

// Map the interface to the concrete typed client so consumers requesting IKatanaService
// receive the properly configured KatanaService instance (with IMemoryCache available).
builder.Services.AddScoped<IKatanaService>(sp => sp.GetRequiredService<KatanaService>());

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
// Register order service so controllers depending on IOrderService can be activated
builder.Services.AddScoped<Katana.Core.Interfaces.IOrderService, Katana.Business.Services.OrderService>();
builder.Services.AddScoped<Katana.Business.Interfaces.IPendingStockAdjustmentService, Katana.Business.Services.PendingStockAdjustmentService>();
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
        // Allow any localhost origin (different dev servers may use 3000,3001 etc.)
        policy.SetIsOriginAllowed(origin =>
                !string.IsNullOrEmpty(origin) && (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost")))
              .AllowAnyHeader()
                            // Allow frontend to read Authorization and other response headers if needed
                            .WithExposedHeaders("Authorization")
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

// In production/staging enable scheduled sync jobs and background flusher. In local
// development these background jobs can make the process noisy and may lead to
// unexpected shutdowns during iteration, so disable them to keep the dev server
// stable and fast to restart.
// Background jobs are disabled by default to keep local development stable.
// To enable background jobs in an environment (e.g., staging/prod), set
// the environment variable ENABLE_BACKGROUND_SERVICES=true.
var enableBackground = string.Equals(Environment.GetEnvironmentVariable("ENABLE_BACKGROUND_SERVICES"), "true", StringComparison.OrdinalIgnoreCase);
if (enableBackground)
{
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
}

// -----------------------------
// Build & Run
// -----------------------------
var app = builder.Build();

// Show which DB connection string we're using (masked) to help local debugging
try
{
    var sqlConn = builder.Configuration.GetConnectionString("SqlServerConnection");
    if (!string.IsNullOrWhiteSpace(sqlConn))
    {
        var masked = Regex.Replace(sqlConn, "(Password=)([^;]+)", "$1*****", RegexOptions.IgnoreCase);
        Console.WriteLine($"Using SqlServerConnection: {masked}");
    }
    else
    {
        Console.WriteLine("No SqlServerConnection configured");
    }
}
catch { /* ignore during boot */ }

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

// Geliştirme ortamında veritabanını otomatik hazırla
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
    try
    {
        if (db.Database.IsSqlite())
        {
            // SQLite için hızlı başlangıç: EnsureCreated (migrasyon setleri provider'a göre değişken)
            db.Database.EnsureCreated();
        }
        else
        {
            // SQL Server için migrasyonları uygula
            db.Database.Migrate();
        }

        // Lightweight dev seed: ensure minimal mapping defaults exist
        if (!db.MappingTables.Any())
        {
            db.MappingTables.AddRange(new Katana.Data.Models.MappingTable
            {
                MappingType = "SKU_ACCOUNT",
                SourceValue = "DEFAULT",
                TargetValue = "600.01",
                Description = "Default account code for unmapped products",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new Katana.Data.Models.MappingTable
            {
                MappingType = "LOCATION_WAREHOUSE",
                SourceValue = "DEFAULT",
                TargetValue = "MAIN",
                Description = "Default warehouse code for unmapped locations",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // Dev kolaylığı: oluşturma başarısız olsa bile uygulama devam etsin (logla)
        Console.WriteLine($"Dev DB init failed: {ex.Message}");
    }
}
app.Run();
