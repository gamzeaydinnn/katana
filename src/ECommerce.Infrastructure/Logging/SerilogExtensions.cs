using Microsoft.Extensions.Hosting;
using Serilog;

namespace ECommerce.Infrastructure.Logging;

public static class SerilogExtensions
{
    public static IHostBuilder UseSerilogConfiguration(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day);
        });
    }
}