using GitHubIntegration.Models;
using GitHubIntegration.Services;
using NSubstitute;
using Octokit;

namespace GitHubIntegration.Tests;

public class GitHubServiceTests
{
    private static readonly RepositoryName Repo = new("octo", "repo");

    private readonly IGitHubClient _client = Substitute.For<IGitHubClient>();
    private readonly GitHubService _service;

    public GitHubServiceTests() => _service = new GitHubService(_client);

    [Fact]
    public async Task Recent_commits_ask_for_one_page_on_the_default_branch()
    {
        await _service.GetRecentCommitsAsync(Repo, 5, branch: null);

        await _client.Repository.Commit.Received().GetAll("octo", "repo",
            Arg.Is<CommitRequest>(request => request.Sha == null),
            Arg.Is<ApiOptions>(options => options.PageSize == 5 && options.PageCount == 1));
    }

    [Fact]
    public async Task Recent_commits_read_the_branch_asked_for()
    {
        await _service.GetRecentCommitsAsync(Repo, 5, "develop");

        await _client.Repository.Commit.Received().GetAll("octo", "repo",
            Arg.Is<CommitRequest>(request => request.Sha == "develop"), Arg.Any<ApiOptions>());
    }

    [Fact]
    public async Task A_file_is_read_from_the_branch_asked_for()
    {
        _client.Repository.Content.GetAllContentsByRef("octo", "repo", "src/app.cs", "develop")
            .Returns([Fakes.File("src/app.cs", "class App {}")]);

        var content = await _service.GetFileContentAsync(Repo, "src/app.cs", "develop");

        Assert.Equal("class App {}", content);
        await _client.Repository.Content.DidNotReceiveWithAnyArgs().GetAllContents(default!, default!, default!);
    }

    [Fact]
    public async Task A_directory_is_not_a_file()
    {
        _client.Repository.Content.GetAllContents("octo", "repo", "src")
            .Returns([Fakes.File("src/only.cs", "x")]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetFileContentAsync(Repo, "src", null));
        Assert.Contains("is a directory", ex.Message);
    }

    [Fact]
    public async Task Browsing_the_root_uses_the_overload_without_a_path()
    {
        _client.Repository.Content.GetAllContents("octo", "repo").Returns([Fakes.Directory("src")]);

        var contents = await _service.BrowseAsync(Repo, path: null, branch: null);

        Assert.Equal("src", Assert.Single(contents).Name);
    }

    [Fact]
    public async Task My_repositories_come_from_user_repos_one_page_newest_first()
    {
        await _service.GetMyRepositoriesAsync();

        await _client.Repository.Received().GetAllForCurrent(
            Arg.Is<RepositoryRequest>(request => request.Sort == RepositorySort.Updated),
            Arg.Is<ApiOptions>(options => options.PageSize == GitHubService.PageSize && options.PageCount == 1));
    }

    [Fact]
    public async Task Pull_requests_ask_for_one_page()
    {
        await _service.GetPullRequestsAsync(Repo, ItemStateFilter.Closed);

        await _client.PullRequest.Received().GetAllForRepository("octo", "repo",
            Arg.Is<PullRequestRequest>(request => request.State == ItemStateFilter.Closed),
            Arg.Is<ApiOptions>(options => options.PageCount == 1));
    }

    [Fact]
    public async Task Code_search_can_be_limited_to_one_repository()
    {
        await _service.SearchCodeAsync("ApiOptions", Repo);

        await _client.Search.Received().SearchCode(
            Arg.Is<SearchCodeRequest>(request => request.Repos.Contains("octo/repo")));
    }
}
