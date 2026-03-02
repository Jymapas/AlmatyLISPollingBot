using DotNetEnv;
using Microsoft.Extensions.Configuration;

namespace AlmatyLISPollingBot.Worker.Configuration;

public static class ConfigurationBootstrapper
{
    public static void ConfigureAppConfiguration(ConfigurationManager configuration)
    {
        Env.TraversePath().Load();

        configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables();
    }
}
