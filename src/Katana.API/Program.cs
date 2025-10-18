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
using Quartz;
using Serilog;
using System.Text;
using Katana.Infrastructure.APIClients;
using Katana.Business.UseCases.Sync;
using Katana.Infrastructure.Notifications;
using Katana.Core.Interfaces;


var builder = WebApplication.CreateBuilder(args);

// -----------------------------
// Logging (Serilog)
// -----------------------------
builder.Host.UseSerilogConfiguration();

// -----------------------------
// Services
// -----------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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
// Database (SQLite)
// -----------------------------
builder.Services.AddDbContext<IntegrationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
});

// -----------------------------
// Configuration Bindings
// -----------------------------
builder.Services.Configure<KatanaApiSettings>(builder.Configuration.GetSection("KatanaApiSettings"));
builder.Services.Configure<LucaApiSettings>(builder.Configuration.GetSection("LucaApiSettings"));

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// -----------------------------
// HTTP Clients
// -----------------------------
builder.Services.AddHttpClient<IKatanaService, KatanaService>();

// -----------------------------
// Repository + UnitOfWork
// -----------------------------
builder.Services.AddScoped(typeof(Katana.Core.Interfaces.IRepository<>), typeof(Repository<>));
//builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// -----------------------------
// Business Services
// -----------------------------
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAccountingService, AccountingService>();
builder.Services.AddScoped<ExtractorService>();
builder.Services.AddScoped<TransformerService>();
builder.Services.AddScoped<LoaderService>();
builder.Services.AddScoped<ISyncService, SyncService>();



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
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// -----------------------------
// Health Checks
// -----------------------------
builder.Services.AddHealthChecks().AddDbContextCheck<IntegrationDbContext>();

// -----------------------------
// Background Services (Worker + Quartz) - Disabled (requires Luca API)
// -----------------------------
// builder.Services.AddHostedService<SyncWorkerService>();

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

// -----------------------------
// Build & Run
// -----------------------------
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Katana-Luca Integration API v1");
    c.RoutePrefix = string.Empty; // Swagger UI ana dizinde
});

app.UseCors("AllowSpecificOrigins");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
