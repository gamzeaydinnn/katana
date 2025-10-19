using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Katana.Data.Context;

/// <summary>
/// Design-time factory so Entity Framework tools can create IntegrationDbContext
/// without relying on the ASP.NET host configuration.
/// </summary>
public class IntegrationDbContextFactory : IDesignTimeDbContextFactory<IntegrationDbContext>
{
    public IntegrationDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        var connectionString = configuration.GetConnectionString("SqlServerConnection")
                               ?? configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("No connection string configured for IntegrationDbContext.");

        var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();

        if (IsSqlServerConnection(connectionString))
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
        else
        {
            optionsBuilder.UseSqlite(connectionString);
        }

        return new IntegrationDbContext(optionsBuilder.Options);
    }

    private static IConfiguration BuildConfiguration()
    {
        var basePath = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var builder = new ConfigurationBuilder()
            .SetBasePath(basePath);

        builder.AddJsonFile("appsettings.json", optional: true);

        if (!string.IsNullOrEmpty(environment))
        {
            builder.AddJsonFile($"appsettings.{environment}.json", optional: true);
        }

        // Fallback to API project configuration files if they exist
        builder.AddJsonFile(Path.Combine("..", "Katana.API", "appsettings.json"), optional: true);
        if (!string.IsNullOrEmpty(environment))
        {
            builder.AddJsonFile(Path.Combine("..", "Katana.API", $"appsettings.{environment}.json"), optional: true);
        }

        builder.AddEnvironmentVariables();

        return builder.Build();
    }

    private static bool IsSqlServerConnection(string connectionString)
    {
        return connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase);
    }
}
