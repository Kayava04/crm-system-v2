using Serilog;

namespace Host.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddLogging(this IHostBuilder host)
    {
        host.UseSerilog((context, config) =>
            config.ReadFrom.Configuration(context.Configuration)
        );

        return host;
    }
}
