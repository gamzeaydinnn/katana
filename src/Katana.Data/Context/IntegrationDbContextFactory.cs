using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Katana.Data.Context;





public class IntegrationDbContextFactory : IDesignTimeDbContextFactory<IntegrationDbContext>
{
    public IntegrationDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();

        
        var sqlServerConnection = configuration.GetConnectionString("SqlServerConnection");

        var optionsBuilder = new DbContextOptionsBuilder<IntegrationDbContext>();

        if (!string.IsNullOrWhiteSpace(sqlServerConnection))
        {
            Console.WriteLine("DesignTimeFactory: Using SqlServerConnection.");
            optionsBuilder.UseSqlServer(sqlServerConnection, sqlOptions => sqlOptions.CommandTimeout(60));
        }
        else
        {
            throw new InvalidOperationException(
                "DesignTimeFactory requires a 'SqlServerConnection' in configuration.");
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
