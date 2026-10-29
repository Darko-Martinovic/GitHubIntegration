using GitHubIntegration.Models;
using GitHubIntegration.Services;
using Octokit;

namespace GitHubIntegration.Tests;

/// <summary>
/// Read-only calls against the real API on public repositories. They run when GITHUB_TOKEN is set
/// (the CI workflow passes the job's token) and are skipped otherwise.
/// </summary>
public class LiveGitHubTests
{
    // octocat/Hello-World's default branch is master, which the old version's hard-coded "main" missed.
    private static readonly RepositoryName HelloWorld = new("octocat", "Hello-World");

    private static GitHubService CreateService()
    {
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        Assert.SkipWhen(string.IsNullOrWhiteSpace(token), "GITHUB_TOKEN is not set.");
        return new GitHubService(new GitHubClient(new ProductHeaderValue(GitHubSettings.ProductName))
        {
            Credentials = new Credentials(token),
        });
    }

    [Fact]
    public async Task Recent_commits_come_from_the_default_branch_whatever_its_name()
    {
        var commits = await CreateService().GetRecentCommitsAsync(HelloWorld, 3, branch: null);

        Assert.InRange(commits.Count, 1, 3);
    }

    [Fact]
    public async Task A_file_is_read_from_a_named_branch()
    {
        var content = await CreateService().GetFileContentAsync(HelloWorld, "README", "master");

        Assert.Contains("Hello World", content);
    }

    [Fact]
    public async Task The_root_of_a_repository_can_be_browsed()
    {
        var contents = await CreateService().BrowseAsync(HelloWorld, path: null, branch: null);

        Assert.Contains(contents, item => item.Name == "README");
    }

    [Fact]
    public async Task Repositories_can_be_searched()
    {
        var result = await CreateService().SearchRepositoriesAsync("octokit.net in:name");

        Assert.Contains(result.Items, repository => repository.FullName == "octokit/octokit.net");
    }
}
