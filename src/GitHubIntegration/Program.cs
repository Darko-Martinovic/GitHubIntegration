using DotNetEnv;
using GitHubIntegration.Models;
using GitHubIntegration.Services;
using Octokit;
using Spectre.Console;

// Look for .env in this folder and its parents; variables already set in the environment win.
Env.TraversePath().NoClobber().Load();

GitHubSettings settings;
try
{
    settings = GitHubSettings.FromEnvironment(Environment.GetEnvironmentVariable);
}
catch (InvalidOperationException ex)
{
    AnsiConsole.MarkupLineInterpolated($"[red]{ex.Message}[/]");
    return 1;
}

var client = new GitHubClient(new ProductHeaderValue(GitHubSettings.ProductName))
{
    Credentials = new Credentials(settings.Token),
};

await new ConsoleUIService(AnsiConsole.Console, new GitHubService(client), settings).RunAsync();
return 0;
