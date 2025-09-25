using ECommerce.API.Middleware;
using ECommerce.Business.Configuration;
using ECommerce.Business.Services;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Data.Repositories;
using ECommerce.Infrastructure.Jobs;
using ECommerce.Infrastructure.Logging;
using ECommerce.Infrastructure.Workers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilogConfiguration();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
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

// Database Configuration
builder.Services.AddDbContext<IntegrationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString);
});

// Configuration
builder.Services.Configure<KatanaApiSettings>(
    builder.Configuration.GetSection(KatanaApiSettings.SectionName));
builder.Services.Configure<LucaApiSettings>(
    builder.Configuration.GetSection(LucaApiSettings.SectionName));
builder.Services.Configure<SyncSettings>(
    builder.Configuration.GetSection(SyncSettings.SectionName));

// HTTP Clients
builder.Services.AddHttpClient<IKatanaService, KatanaService>();
builder.Services.AddHttpClient<ILucaService, LucaService>();

// Repository and UnitOfWork
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Business Services
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IMappingService, MappingService>();

// Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

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

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IntegrationDbContext>();

// Background Services
builder.Services.AddHostedService<SyncWorkerService>();

// Quartz.NET
builder.Services.AddQuartz(q =>
{
    
    // Stock sync job - every 6 hours
    var stockJobKey = new JobKey("StockSyncJob");
    q.AddJob<SyncJob>(opts => opts
        .WithIdentity(stockJobKey)
        .UsingJobData("SyncType", "STOCK"));
    
    q.AddTrigger(opts => opts
        .ForJob(stockJobKey)
        .WithIdentity("StockSyncTrigger")
        .WithCronSchedule("0 0 */6 * * ?"));
    
    // Invoice sync job - every 4 hours
    var invoiceJobKey = new JobKey("InvoiceSyncJob");
    q.AddJob<SyncJob>(opts => opts
        .WithIdentity(invoiceJobKey)
        .UsingJobData("SyncType", "INVOICE"));
    
    q.AddTrigger(opts => opts
        .ForJob(invoiceJobKey)
        .WithIdentity("InvoiceSyncTrigger")
        .WithCronSchedule("0 0 */4 * * ?"));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Katana-Luca Integration API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigins");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AuthMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();