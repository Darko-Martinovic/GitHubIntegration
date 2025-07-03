using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using GitHubIntegration.Services;
using GitHubIntegration.Models;
using DotNetEnv;

namespace GitHubIntegration;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Load .env file for local development
            Env.Load();

            // Build configuration
            var configuration = ConfigurationService.BuildConfiguration();
            var gitHubSettings = ConfigurationService.GetGitHubSettings(configuration);

            // Set up dependency injection
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register GitHub settings as singleton
                    services.AddSingleton(gitHubSettings);

                    // Register services
                    services.AddSingleton<IGitHubService, GitHubService>();
                    services.AddSingleton<ConsoleUIService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            // Get the console UI service and start the application
            var consoleUI = host.Services.GetRequiredService<ConsoleUIService>();
            await consoleUI.ShowMainMenuAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start application: {ex.Message}");
            Console.WriteLine("Please ensure your .env file contains the required GitHub configuration.");
            Console.WriteLine("Required environment variables:");
            Console.WriteLine("- GITHUB_TOKEN: Your GitHub Personal Access Token");
            Console.WriteLine("- GITHUB_REPO_OWNER: Your GitHub username");
            Console.WriteLine("- GITHUB_REPO_NAME: Default repository name (optional)");
        }
    }
}
