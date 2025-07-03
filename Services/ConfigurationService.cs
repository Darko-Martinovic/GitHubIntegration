using Microsoft.Extensions.Configuration;
using GitHubIntegration.Models;

namespace GitHubIntegration.Services;

public static class ConfigurationService
{
    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables()
            .Build();
    }

    public static GitHubSettings GetGitHubSettings(IConfiguration configuration)
    {
        return new GitHubSettings
        {
            Token = configuration["GITHUB_TOKEN"] ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable is required"),
            DefaultOwner = configuration["GITHUB_REPO_OWNER"] ?? throw new InvalidOperationException("GITHUB_REPO_OWNER environment variable is required"),
            DefaultRepo = configuration["GITHUB_REPO_NAME"] ?? string.Empty,
            UserAgent = "GitHubIntegrationConsole",
            MaxResults = 50
        };
    }
}