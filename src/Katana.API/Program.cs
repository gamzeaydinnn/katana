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
using System.Threading.Tasks;
using Katana.API.Workers;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Port Configuration
// -----------------------------
var envUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
var configuredUrls = builder.Configuration["Urls"];
if (string.IsNullOrWhiteSpace(envUrls) && string.IsNullOrWhiteSpace(configuredUrls))
{
    int[] preferred = { 5055, 5056, 5057, 5058, 5059 };
    int chosen = preferred.First(p =>
    {
        try
        {
            var l = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, p);
            l.Start();
            l.Stop();
            return true;
        }
        catch { return false; }
    });

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
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// -----------------------------
// Swagger
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
        Description = "X-API-Key header required",
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// -----------------------------
// Database (SQL Server only)
// -----------------------------
builder.Services.AddDbContext<IntegrationDbContext>(options =>
{
    var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServerConnection");

    if (string.IsNullOrWhiteSpace(sqlServerConnection))
        throw new InvalidOperationException("SqlServerConnection is required for all environments.");

    options.UseSqlServer(sqlServerConnection, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
});

// -----------------------------
// Config Bindings
// -----------------------------
builder.Services.Configure<KatanaApiSettings>(builder.Configuration.GetSection("KatanaApi"));
builder.Services.Configure<LucaApiSettings>(builder.Configuration.GetSection("LucaApi"));
builder.Services.AddAuthorization();

// -----------------------------
// HTTP Clients
// -----------------------------
builder.Services.AddHttpClient<KatanaService>((sp, client) =>
{
    var s = sp.GetRequiredService<IOptions<KatanaApiSettings>>().Value;
    client.BaseAddress = new Uri(s.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", s.ApiKey?.Trim());
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddScoped<IKatanaService>(sp => sp.GetRequiredService<KatanaService>());

builder.Services.AddHttpClient<ILucaService, LucaService>((sp, client) =>
{
    var s = sp.GetRequiredService<IOptions<LucaApiSettings>>().Value;
    client.BaseAddress = new Uri(s.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrEmpty(s.ApiKey) && !s.UseTokenAuth)
        client.DefaultRequestHeaders.Add("X-API-Key", s.ApiKey);
});

builder.Services.AddSingleton<ILucaCookieJarStore, LucaCookieJarStore>();

// -----------------------------
// Repositories + Services
// -----------------------------
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IRepository<Category>, CategoryRepository>();
builder.Services.AddScoped<IExtractorService, ExtractorService>();
builder.Services.AddScoped<ITransformerService, TransformerService>();
builder.Services.AddScoped<ILoaderService, LoaderService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IIntegrationService>(sp => (IIntegrationService)sp.GetRequiredService<ISyncService>());
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPendingStockAdjustmentService, PendingStockAdjustmentService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IErrorHandler, ErrorHandlerService>();
builder.Services.AddSingleton<Katana.Infrastructure.Services.CacheService>();
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IPendingNotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AdminService>();

// -----------------------------
// JWT Authentication
// -----------------------------
var jwt = builder.Configuration.GetSection("Jwt");
var key = jwt["Key"] ?? throw new InvalidOperationException("JWT Key not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
        };
    });
builder.Services.AddAuthorization();

// -----------------------------
// CORS
// -----------------------------
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowFrontend", p =>
        p.SetIsOriginAllowed(origin =>
            !string.IsNullOrEmpty(origin) &&
            (origin.StartsWith("http://localhost") || origin.StartsWith("https://localhost")))
         .AllowAnyHeader()
         .WithExposedHeaders("Authorization")
         .AllowAnyMethod()
         .AllowCredentials());
});

// -----------------------------
// Health Checks
// -----------------------------
builder.Services.AddHealthChecks().AddDbContextCheck<IntegrationDbContext>();

// -----------------------------
// Background Services
// -----------------------------
var enableBackground = string.Equals(Environment.GetEnvironmentVariable("ENABLE_BACKGROUND_SERVICES"), "true", StringComparison.OrdinalIgnoreCase);
if (enableBackground)
{
    builder.Services.AddQuartz(q =>
    {
        var stockJobKey = new JobKey("StockSyncJob");
        q.AddJob<SyncJob>(o => o.WithIdentity(stockJobKey).UsingJobData("SyncType", "STOCK"));
        q.AddTrigger(o => o.ForJob(stockJobKey).WithIdentity("StockSyncTrigger").WithCronSchedule("0 0 */6 * * ?"));
    });
    builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    builder.Services.AddSingleton<Katana.Core.Services.PendingDbWriteQueue>();
    builder.Services.AddHostedService<Katana.Infrastructure.Workers.RetryPendingDbWritesService>();
}

builder.Services.AddHostedService<HourlyMetricsAggregator>();
builder.Services.AddHostedService<LogRetentionService>();
builder.Services.AddHostedService<FailedNotificationProcessor>();

// -----------------------------
// Build & Run
// -----------------------------
var app = builder.Build();

// Connection string info
try
{
    var sqlConn = builder.Configuration.GetConnectionString("SqlServerConnection");
    if (!string.IsNullOrWhiteSpace(sqlConn))
    {
        var masked = Regex.Replace(sqlConn, "(Password=)([^;]+)", "$1*****", RegexOptions.IgnoreCase);
        Console.WriteLine($"Using SqlServerConnection: {masked}");
    }
}
catch { }

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Katana-Luca Integration API v1");
    c.RoutePrefix = string.Empty;
});

app.UseRouting();
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
app.UseCors("AllowFrontend");
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<Katana.API.Hubs.NotificationHub>("/hubs/notifications");

// Auto-migrate DB (SQL Server only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
    try
    {
        db.Database.Migrate();

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
                UpdatedAt = DateTime.UtcNow
            },
            new Katana.Data.Models.MappingTable
            {
                MappingType = "LOCATION_WAREHOUSE",
                SourceValue = "DEFAULT",
                TargetValue = "MAIN",
                Description = "Default warehouse code for unmapped locations",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Dev DB init failed: {ex.Message}");
    }
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Run();
