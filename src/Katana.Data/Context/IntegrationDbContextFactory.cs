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

        // Follow the same logic as Program.cs: prefer explicit SqlServerConnection.
        var sqlServerConnection = configuration.GetConnectionString("SqlServerConnection");
        var sqliteConnection = configuration.GetConnectionString("DefaultConnection");
        var allowSqlite = string.Equals(Environment.GetEnvironmentVariable("ALLOW_SQLITE_FALLBACK"), "true", StringComparison.OrdinalIgnoreCase);

        var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();

        if (!string.IsNullOrWhiteSpace(sqlServerConnection))
        {
            Console.WriteLine("DesignTimeFactory: Using SqlServerConnection.");
            optionsBuilder.UseSqlServer(sqlServerConnection, sqlOptions => sqlOptions.CommandTimeout(60));
        }
        else if (allowSqlite && !string.IsNullOrWhiteSpace(sqliteConnection))
        {
            Console.WriteLine("DesignTimeFactory: Using SQLite fallback (DefaultConnection).");
            optionsBuilder.UseSqlite(sqliteConnection);
        }
        else
        {
            // Provide a clear error so developers know how to proceed.
            throw new InvalidOperationException(
                "DesignTimeFactory requires a 'SqlServerConnection' in configuration. " +
                "To use SQLite for migrations, set environment variable ALLOW_SQLITE_FALLBACK=true and provide 'DefaultConnection'."
            );
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
