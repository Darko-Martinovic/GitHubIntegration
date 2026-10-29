using GitHubIntegration.Models;

namespace GitHubIntegration.Tests;

public class ModelTests
{
    [Theory]
    [InlineData("octokit/octokit.net", "octokit", "octokit.net")]
    [InlineData(" octo / repo ", "octo", "repo")]
    public void A_repository_name_is_owner_slash_name(string text, string owner, string name)
    {
        Assert.True(RepositoryName.TryParse(text, out var repository));
        Assert.Equal(new RepositoryName(owner, name), repository);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("octokit")]
    [InlineData("octokit/")]
    [InlineData("a/b/c")]
    public void Anything_else_is_not_a_repository_name(string? text)
    {
        Assert.False(RepositoryName.TryParse(text, out _));
    }

    [Fact]
    public void The_token_is_required()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => GitHubSettings.FromEnvironment(_ => null));
        Assert.Contains("GITHUB_TOKEN", ex.Message);
    }

    [Fact]
    public void The_default_repository_needs_both_owner_and_name()
    {
        var both = new Dictionary<string, string> { ["GITHUB_TOKEN"] = "t", ["GITHUB_REPO_OWNER"] = "octo", ["GITHUB_REPO_NAME"] = "repo" };
        var ownerOnly = new Dictionary<string, string> { ["GITHUB_TOKEN"] = "t", ["GITHUB_REPO_OWNER"] = "octo" };

        Assert.Equal(new RepositoryName("octo", "repo"), GitHubSettings.FromEnvironment(both.GetValueOrDefault).DefaultRepository);
        Assert.Null(GitHubSettings.FromEnvironment(ownerOnly.GetValueOrDefault).DefaultRepository);
    }
}
