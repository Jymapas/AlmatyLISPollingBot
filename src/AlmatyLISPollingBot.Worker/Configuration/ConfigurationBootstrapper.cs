using DotNetEnv;
using Microsoft.Extensions.Configuration;

namespace AlmatyLISPollingBot.Worker.Configuration;

public static class ConfigurationBootstrapper
{
    public static void ConfigureAppConfiguration(ConfigurationManager configuration)
    {
        Env.TraversePath().Load();
        var secretsPath = FindRequiredSecretsEnv();
        Env.Load(secretsPath);

        configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables();
    }

    private static string FindRequiredSecretsEnv()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, "secrets.env");
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException(
            "Missing required secrets.env file. Create it from secrets.env.example before starting the bot.");
    }
}
