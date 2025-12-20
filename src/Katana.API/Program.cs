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
using System.Reflection;
using System.Linq;
using Microsoft.Extensions.Options;
using Quartz;
using Serilog;
using System.Text;
using Katana.Infrastructure.APIClients;
using Katana.Infrastructure.Utils;
using Katana.Infrastructure.Notifications;
using Katana.API.Notifications;
using Katana.Core.Interfaces;
using Katana.Core.Entities;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Katana.API.Workers;
using Katana.API.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// CORS ayarları - builder.Services.AddControllers()'dan ÖNCE
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://127.0.0.1:3000",
                "http://localhost:3001",
                "http://127.0.0.1:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ASPNETCORE_URLS varsa, otomatik kullanılır ve bu blok atlanır
// Eğer yoksa, appsettings'deki Kestrel config veya fallback'e başvur
var aspnetcoreUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (string.IsNullOrWhiteSpace(aspnetcoreUrls))
{
    var configuredUrl = builder.Configuration["Kestrel:Endpoints:Http:Url"];
    var fallbackUrl = string.IsNullOrWhiteSpace(configuredUrl) ? "http://localhost:5055" : configuredUrl;
    builder.WebHost.UseUrls(fallbackUrl);
}

builder.Host.UseSerilogConfiguration();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Katana-Luca Integration API",
        Version = "v1",
        Description = "API for managing integration between Katana MRP/ERP and Luca Accounting systems"
    });

    
    var xmlApi = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlApi)) c.IncludeXmlComments(xmlApi, includeControllerXmlComments: true);
    var xmlCore = Path.Combine(AppContext.BaseDirectory, "Katana.Core.xml");
    if (File.Exists(xmlCore)) c.IncludeXmlComments(xmlCore);

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "X-API-Key header required",
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    
    c.OperationFilter<Katana.API.Swagger.OperationFilters.AddAuthResponsesOperationFilter>();
});

builder.Services.AddDbContext<IntegrationDbContext>(options =>
{
    var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServerConnection");

    if (string.IsNullOrWhiteSpace(sqlServerConnection))
        throw new InvalidOperationException("SqlServerConnection is required for all environments.");

    options.UseSqlServer(sqlServerConnection, sqlOptions =>
        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
});

builder.Services.Configure<KatanaApiSettings>(builder.Configuration.GetSection("KatanaApi"));
builder.Services.Configure<LucaApiSettings>(builder.Configuration.GetSection("LucaApi"));
builder.Services.Configure<SyncSettings>(builder.Configuration.GetSection(SyncSettings.SectionName));
builder.Services.Configure<InventorySettings>(builder.Configuration.GetSection(InventorySettings.SectionName));
builder.Services.Configure<CatalogVisibilitySettings>(builder.Configuration.GetSection(CatalogVisibilitySettings.SectionName));
builder.Services.Configure<KatanaMappingSettings>(builder.Configuration.GetSection("Mapping:Katana"));
builder.Services.AddAuthorization();

builder.Services.AddHttpClient<IKatanaService, KatanaService>((sp, client) =>
{
    var s = sp.GetRequiredService<IOptions<KatanaApiSettings>>().Value;
    client.BaseAddress = new Uri(s.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(s.TimeoutSeconds);
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", s.ApiKey?.Trim());
    
    // ❌ Don't add Accept header - it might cause issues
    // client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
})
    .AddHttpMessageHandler<RateLimitHandler>();
builder.Services.AddScoped<IKatanaApiClient, KatanaApiClient>();
builder.Services.AddScoped<IKatanaStockService, KatanaStockService>();

builder.Services.AddHttpClient<ILucaService, LucaService>((sp, client) =>
{
    var s = sp.GetRequiredService<IOptions<LucaApiSettings>>().Value;
    client.BaseAddress = new Uri(s.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(120); // Increased timeout for Koza API
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    if (!string.IsNullOrEmpty(s.ApiKey) && !s.UseTokenAuth)
        client.DefaultRequestHeaders.Add("X-API-Key", s.ApiKey);
})
    // Cookie-based auth relies on persistent cookies; attach a CookieContainer to preserve them.
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var s = sp.GetRequiredService<IOptions<LucaApiSettings>>().Value;
        var jar = sp.GetRequiredService<ILucaCookieJarStore>();
        var baseUrlKey = string.IsNullOrWhiteSpace(s.BaseUrl) ? "default" : s.BaseUrl.Trim();
        var cookieJarKey = $"LucaCookieJar:{baseUrlKey}";

        return new SocketsHttpHandler
        {
            CookieContainer = jar.GetOrCreate(cookieJarKey),
            UseCookies = true,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 10,
            // SSL/TLS settings for Koza API
            SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            }
        };
    })
    .AddHttpMessageHandler<RateLimitHandler>()
    .AddHttpDebugLogging();

builder.Services.AddTransient<HttpDebugLoggingHandler>();
builder.Services.AddTransient<RateLimitHandler>();

builder.Services.AddSingleton<ILucaCookieJarStore, LucaCookieJarStore>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IRepository<Category>, CategoryRepository>();

// Mapping Repositories - Order ve StockMovement sync için
builder.Services.AddScoped<IOrderMappingRepository, OrderMappingRepository>();
builder.Services.AddScoped<IStockMovementMappingRepository, StockMovementMappingRepository>();
builder.Services.AddScoped<IVariantMappingRepository, VariantMappingRepository>();
builder.Services.AddScoped<IVariantMappingService, VariantMappingService>();

builder.Services.AddScoped<IExtractorService, ExtractorService>();
builder.Services.AddScoped<ITransformerService, TransformerService>();
builder.Services.AddScoped<ILoaderService, LoaderService>();
builder.Services.AddScoped<ISyncService, SyncService>();
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

// Sync Services - Order Invoice ve Stock Movement entegrasyonları için
builder.Services.AddScoped<IOrderInvoiceSyncService, OrderInvoiceSyncService>();
builder.Services.AddScoped<IStockMovementSyncService, StockMovementSyncService>();

// Query Services - Controller'dan DB logic'i ayırma
builder.Services.AddScoped<IOrderSyncQueryService, OrderSyncQueryService>();

builder.Services.AddSingleton<Katana.Infrastructure.Services.CacheService>();
builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuditLoggerService, AuditLoggerService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IEventPublisher, EventPublisher>();
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<IMappingSyncService, MappingSyncService>();
builder.Services.AddScoped<ILocationMappingService, LocationMappingService>();
builder.Services.AddScoped<ICustomerMappingService, CustomerMappingService>();
builder.Services.AddScoped<IDocumentMappingService, DocumentMappingService>();
builder.Services.AddScoped<ITaxRateMappingService, TaxRateMappingService>();
builder.Services.AddScoped<IUoMMappingService, UoMMappingService>();
builder.Services.AddScoped<IIntegrationTestService, IntegrationTestService>();
builder.Services.AddScoped<IDataCorrectionService, DataCorrectionService>();
builder.Services.AddScoped<OrderInvoiceSyncService>();

// Deduplication Services
builder.Services.Configure<Katana.Core.DTOs.DeduplicationRules>(builder.Configuration.GetSection("Deduplication"));
builder.Services.AddScoped<Katana.Business.Services.Deduplication.IDuplicateDetector, Katana.Business.Services.Deduplication.DuplicateDetector>();
builder.Services.AddScoped<Katana.Business.Services.Deduplication.ICanonicalSelector, Katana.Business.Services.Deduplication.CanonicalSelector>();
builder.Services.AddScoped<Katana.Business.Services.Deduplication.IDeduplicationService, Katana.Business.Services.Deduplication.DeduplicationService>();

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
});
builder.Services.AddScoped<IPendingNotificationPublisher, SignalRNotificationPublisher>();
builder.Services.AddSingleton<Katana.Core.Interfaces.ISyncProgressReporter, Katana.API.Notifications.SignalRSyncProgressReporter>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<AdminService>();
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "JWT Authentication failed for {Path}. Token: {Token}", 
                    context.HttpContext.Request.Path, 
                    context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Substring(0, Math.Min(20, context.Request.Headers["Authorization"].ToString().Length)));
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogDebug("JWT Token validated successfully for {User}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHealthChecks().AddDbContextCheck<IntegrationDbContext>();

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
	    builder.Services.AddHostedService<Katana.API.Workers.KatanaSalesOrderSyncWorker>();
	}

builder.Services.AddHostedService<HourlyMetricsAggregator>();
builder.Services.AddHostedService<LogRetentionService>();
builder.Services.AddHostedService<FailedNotificationProcessor>();
// builder.Services.AddHostedService<AutoSyncWorker>(); // Geçici olarak kapatıldı - çift sync sorunu

var app = builder.Build();

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
// --- CORS / request debug middleware (geçici) ---
app.Use(async (context, next) =>
{
    var origin = context.Request.Headers["Origin"].FirstOrDefault() ?? "<none>";
    Console.WriteLine($"[CORS DEBUG] {context.Request.Method} {context.Request.Path} Origin:{origin}");
    try
    {
        await next();
        Console.WriteLine($"[CORS DEBUG] Response {context.Response.StatusCode} for {context.Request.Method} {context.Request.Path}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[CORS ERROR] Exception for {context.Request.Method} {context.Request.Path} Origin:{origin} : {ex}");
        throw;
    }
});
// --- end debug middleware ---
app.UseRouting();
app.UseCors("DevCors");  // CORS middleware - routing'den sonra, auth'dan önce

// Development: 500 hatalarında bile CORS header'ları döndür
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Unhandled exception in request pipeline");

            // Origin kontrolü - localhost:3000'den geliyorsa CORS header ekle
            var origin = context.Request.Headers["Origin"].ToString();
            if (!string.IsNullOrEmpty(origin) && 
                (origin.Contains("localhost:3000") || origin.Contains("127.0.0.1:3000") ||
                 origin.Contains("localhost:3001") || origin.Contains("127.0.0.1:3001")))
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            }

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Internal Server Error",
                message = ex.Message,
                type = ex.GetType().Name,
                stackTrace = app.Environment.IsDevelopment() ? ex.StackTrace : null
            });
        }
    });
}

app.UseWebSockets();
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers().RequireCors("DevCors");
app.MapHealthChecks("/health");
app.MapHub<Katana.API.Hubs.NotificationHub>("/hubs/notifications").RequireCors("DevCors");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();
    var inventoryOptions = scope.ServiceProvider.GetRequiredService<IOptions<InventorySettings>>();
    var defaultWarehouseCode = inventoryOptions.Value.DefaultWarehouseCode ?? "MAIN";
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
                TargetValue = defaultWarehouseCode,
                Description = "Default warehouse code for unmapped locations",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
        else
        {
            var defaultWarehouse = db.MappingTables.FirstOrDefault(m =>
                m.MappingType == "LOCATION_WAREHOUSE" &&
                m.SourceValue == "DEFAULT");
            if (defaultWarehouse == null)
            {
                db.MappingTables.Add(new Katana.Data.Models.MappingTable
                {
                    MappingType = "LOCATION_WAREHOUSE",
                    SourceValue = "DEFAULT",
                    TargetValue = defaultWarehouseCode,
                    Description = "Default warehouse code for unmapped locations",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }
            else if (string.IsNullOrWhiteSpace(defaultWarehouse.TargetValue))
            {
                defaultWarehouse.TargetValue = defaultWarehouseCode;
                defaultWarehouse.Description ??= "Default warehouse code for unmapped locations";
                defaultWarehouse.UpdatedAt = DateTime.UtcNow;
                db.SaveChanges();
            }
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

await EnsureDefaultAdminUserAsync(app.Services, app.Configuration, app.Logger);

// Startup sırasında Luca cache'ini ısıt (kullanıcı ilk sayfayı açtığında beklemsin diye)
_ = Task.Run(async () =>
{
    try
    {
        using var scope = app.Services.CreateScope();
        var lucaService = scope.ServiceProvider.GetRequiredService<ILucaService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        await Task.Delay(5000); // API tamamen hazır olana kadar bekle
        
        logger.LogInformation("Luca stok kartları cache'i ısıtılıyor...");
        var cards = await lucaService.ListStockCardsSimpleAsync(null, null, CancellationToken.None);
        logger.LogInformation("Luca cache hazır: {Count} stok kartı yüklendi", cards?.Count ?? 0);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Luca cache warm-up başarısız: {ex.Message}");
    }
});

app.Run();

static async Task EnsureDefaultAdminUserAsync(
    IServiceProvider services,
    IConfiguration configuration,
    Microsoft.Extensions.Logging.ILogger logger)
{
    var username = configuration["AuthSettings:AdminUsername"]?.Trim();
    var password = configuration["AuthSettings:AdminPassword"];

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        logger.LogWarning("AuthSettings:AdminUsername or AdminPassword is not configured; default admin user not ensured.");
        return;
    }

    try
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntegrationDbContext>();

        var passwordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            db.Users.Add(new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Role = "Admin",
                Email = string.IsNullOrWhiteSpace(username) ? string.Empty : $"{username}@katana.local",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Default admin user '{Username}' created from AuthSettings.", username);
            return;
        }

        var requiresUpdate = user.PasswordHash != passwordHash || !user.IsActive || string.IsNullOrWhiteSpace(user.Role);
        if (requiresUpdate)
        {
            user.PasswordHash = passwordHash;
            user.IsActive = true;
            if (string.IsNullOrWhiteSpace(user.Role))
            {
                user.Role = "Admin";
            }
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            logger.LogInformation("Default admin user '{Username}' synchronized with AuthSettings.", username);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure default admin user exists.");
    }
}
public partial class Program { }
