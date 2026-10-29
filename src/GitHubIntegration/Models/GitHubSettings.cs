namespace GitHubIntegration.Models;

/// <summary>
/// Settings read from environment variables, which Program loads from a .env file when there is one.
/// </summary>
public sealed record GitHubSettings(string Token, RepositoryName? DefaultRepository)
{
    /// <summary>The User-Agent GitHub asks every API client to send.</summary>
    public const string ProductName = "GitHubIntegrationConsole";

    public static GitHubSettings FromEnvironment(Func<string, string?> read)
    {
        var token = read("GITHUB_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "GITHUB_TOKEN is not set. Copy .env.example to .env and put a personal access token in it.");
        }

        var owner = read("GITHUB_REPO_OWNER");
        var name = read("GITHUB_REPO_NAME");
        var defaultRepository = string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(name)
            ? null
            : new RepositoryName(owner.Trim(), name.Trim());

        return new GitHubSettings(token.Trim(), defaultRepository);
    }
}
