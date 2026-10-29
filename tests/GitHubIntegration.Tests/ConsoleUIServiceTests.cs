using System.Net;
using GitHubIntegration.Models;
using GitHubIntegration.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Octokit;
using Spectre.Console.Testing;

namespace GitHubIntegration.Tests;

public class ConsoleUIServiceTests
{
    private static readonly RepositoryName Repo = new("octo", "repo");

    private readonly TestConsole _console = new();
    private readonly IGitHubService _github = Substitute.For<IGitHubService>();

    public ConsoleUIServiceTests()
    {
        _console.Interactive();
        _console.Profile.Width = 200;
    }

    private ConsoleUIService CreateUi(RepositoryName? defaultRepository = null) =>
        new(_console, _github, new GitHubSettings("token", defaultRepository));

    [Fact]
    public async Task A_pull_request_title_in_brackets_is_shown_as_text()
    {
        _github.GetPullRequestsAsync(Repo, ItemStateFilter.Open).Returns([Fakes.PullRequest(7, "[WIP] fix the parser")]);
        _console.Input.PushTextWithEnter("octo/repo");
        _console.Input.PushKey(ConsoleKey.Enter);

        await CreateUi().ListPullRequestsAsync();

        Assert.Contains("[WIP] fix the parser", _console.Output);
    }

    [Fact]
    public async Task Code_with_brackets_is_shown_and_the_branch_can_be_left_empty()
    {
        _github.GetFileContentAsync(Repo, "src/app.cs", null).Returns("int[] values = new int[3];");
        _console.Input.PushTextWithEnter("octo/repo");
        _console.Input.PushTextWithEnter("src/app.cs");
        _console.Input.PushTextWithEnter("");

        await CreateUi().ShowFileAsync();

        Assert.Contains("int[] values = new int[3];", _console.Output);
    }

    [Fact]
    public async Task The_branch_given_is_passed_on()
    {
        _github.GetFileContentAsync(Repo, "README.md", "develop").Returns("hello");
        _console.Input.PushTextWithEnter("octo/repo");
        _console.Input.PushTextWithEnter("README.md");
        _console.Input.PushTextWithEnter("develop");

        await CreateUi().ShowFileAsync();

        await _github.Received().GetFileContentAsync(Repo, "README.md", "develop");
    }

    [Fact]
    public async Task The_configured_repository_is_the_default_answer()
    {
        _github.GetStatisticsAsync(Repo).Returns(new RepositoryStatistics(3, new Dictionary<string, long> { ["C#"] = 300, ["Shell"] = 100 }, 2, 1));
        _console.Input.PushTextWithEnter("");

        await CreateUi(defaultRepository: Repo).ShowStatisticsAsync();

        Assert.Contains("75.0%", _console.Output);
    }

    [Fact]
    public async Task A_404_is_explained()
    {
        _github.GetStatisticsAsync(Repo).ThrowsAsync(new NotFoundException("Not Found", HttpStatusCode.NotFound));
        _console.Input.PushTextWithEnter("octo/repo");
        var ui = CreateUi();

        await ui.RunScreenAsync(ui.ShowStatisticsAsync);

        Assert.Contains("Not found (404)", _console.Output);
    }

    [Fact]
    public async Task Nothing_is_posted_without_confirmation()
    {
        _console.Input.PushTextWithEnter("octo/repo");
        _console.Input.PushTextWithEnter("7");
        _console.Input.PushTextWithEnter("Looks good");
        _console.Input.PushTextWithEnter("n");

        await CreateUi().CommentOnPullRequestAsync();

        await _github.DidNotReceiveWithAnyArgs().CommentOnPullRequestAsync(default!, default, default!);
        Assert.Contains("Nothing was posted.", _console.Output);
    }

    [Fact]
    public async Task Exit_leaves_the_menu()
    {
        _console.Input.PushKey(ConsoleKey.End);
        _console.Input.PushKey(ConsoleKey.Enter);

        await CreateUi().RunAsync();

        await _github.DidNotReceiveWithAnyArgs().GetCurrentUserAsync();
    }
}
