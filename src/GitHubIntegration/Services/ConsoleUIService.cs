using System.Net;
using GitHubIntegration.Models;
using Octokit;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace GitHubIntegration.Services;

/// <summary>
/// The menu and its screens. Everything GitHub returns is written as plain Text, never as markup:
/// a pull request titled "[WIP] ..." or a file containing "int[]" would otherwise be read as a style tag.
/// </summary>
public sealed class ConsoleUIService(IAnsiConsole console, IGitHubService github, GitHubSettings settings)
{
    private RepositoryName? _lastRepository = settings.DefaultRepository;

    private sealed record MenuItem(string Label, Func<Task>? Action = null);

    public async Task RunAsync()
    {
        var menu = BuildMenu();
        while (true)
        {
            console.Clear();
            console.Write(new Rule("[bold]GitHub Integration[/]").LeftJustified());
            var choice = console.Prompt(menu);
            if (choice.Action is null)
            {
                return;
            }

            console.WriteLine();
            await RunScreenAsync(choice.Action);
            console.WriteLine();
            console.MarkupLine("[grey]Press any key to return to the menu.[/]");
            console.Input.ReadKey(intercept: true);
        }
    }

    internal async Task RunScreenAsync(Func<Task> screen)
    {
        try
        {
            await screen();
        }
        catch (ApiException ex)
        {
            console.MarkupLineInterpolated($"[red]{Describe(ex)}[/]");
        }
        catch (HttpRequestException ex)
        {
            console.MarkupLineInterpolated($"[red]Could not reach GitHub: {ex.Message}[/]");
        }
        catch (InvalidOperationException ex)
        {
            console.MarkupLineInterpolated($"[red]{ex.Message}[/]");
        }
    }

    internal static string Describe(ApiException ex) => ex switch
    {
        RateLimitExceededException limit =>
            $"The rate limit is used up ({limit.Limit} requests). It resets at {limit.Reset.ToLocalTime():HH:mm}.",
        AuthorizationException => "GitHub rejected the token (401). Check GITHUB_TOKEN and its expiry date.",
        ForbiddenException => $"GitHub refused the request (403): {ex.Message}. The token may lack a permission.",
        NotFoundException =>
            "Not found (404). Check the owner, name, branch or path. GitHub also answers 404 for a private "
            + "repository the token can't see.",
        ApiValidationException => $"GitHub rejected the input (422): {ex.ApiError?.Errors?.FirstOrDefault()?.Message ?? ex.Message}",
        _ when ex.StatusCode == HttpStatusCode.Conflict => $"GitHub reported a conflict (409): {ex.Message}",
        _ => $"GitHub answered {(int)ex.StatusCode}: {ex.Message}",
    };

    private SelectionPrompt<MenuItem> BuildMenu()
    {
        var menu = new SelectionPrompt<MenuItem>()
            .Title("Choose an action:")
            .PageSize(20)
            .Mode(SelectionMode.Leaf)
            .UseConverter(item => item.Action is null && item.Label != "Exit" ? $"[bold]{item.Label}[/]" : item.Label);

        menu.AddChoiceGroup(new MenuItem("Account"),
            new MenuItem("Show the signed-in user and rate limits", ShowUserAsync),
            new MenuItem("List my repositories", ListMyRepositoriesAsync));
        menu.AddChoiceGroup(new MenuItem("Repository"),
            new MenuItem("Show statistics", ShowStatisticsAsync),
            new MenuItem("Browse files", BrowseAsync),
            new MenuItem("Show a file", ShowFileAsync));
        menu.AddChoiceGroup(new MenuItem("Commits"),
            new MenuItem("List recent commits", ListCommitsAsync),
            new MenuItem("Show a commit", ShowCommitAsync));
        menu.AddChoiceGroup(new MenuItem("Pull requests"),
            new MenuItem("List pull requests", ListPullRequestsAsync),
            new MenuItem("Show a pull request", ShowPullRequestAsync),
            new MenuItem("Comment on a pull request", CommentOnPullRequestAsync),
            new MenuItem("Review a pull request", ReviewPullRequestAsync));
        menu.AddChoiceGroup(new MenuItem("Search"),
            new MenuItem("Search code", SearchCodeAsync),
            new MenuItem("Search repositories", SearchRepositoriesAsync));
        menu.AddChoice(new MenuItem("Exit"));
        return menu;
    }

    internal async Task ShowUserAsync()
    {
        var user = await LoadAsync("Reading the signed-in user", github.GetCurrentUserAsync);
        var scopes = github.GetTokenScopes();
        var limits = await LoadAsync("Reading rate limits", github.GetRateLimitsAsync);

        var table = NewTable("Property", "Value");
        AddRow(table, "Login", user.Login);
        AddRow(table, "Name", user.Name ?? "-");
        AddRow(table, "Email", user.Email ?? "- (private, or the token lacks user:email)");
        AddRow(table, "Location", user.Location ?? "-");
        AddRow(table, "Public repositories", user.PublicRepos.ToString());
        AddRow(table, "Followers / following", $"{user.Followers} / {user.Following}");
        AddRow(table, "Account created", user.CreatedAt.ToString("yyyy-MM-dd"));
        AddRow(table, "Token scopes", scopes.Count == 0 ? "- (fine-grained token)" : string.Join(", ", scopes));
        AddRow(table, "Core API calls left", Describe(limits.Resources.Core));
        AddRow(table, "Search API calls left", Describe(limits.Resources.Search));
        console.Write(table);
    }

    internal async Task ListMyRepositoriesAsync()
    {
        var repositories = await LoadAsync("Reading your repositories", github.GetMyRepositoriesAsync);
        console.MarkupLineInterpolated(
            $"The {repositories.Count} most recently updated repositories you own, collaborate on or reach through an organisation:");

        var table = NewTable("Repository", "Language", "Stars", "Forks", "Visibility", "Updated");
        foreach (var repository in repositories)
        {
            AddRow(table,
                repository.FullName,
                repository.Language ?? "-",
                repository.StargazersCount.ToString(),
                repository.ForksCount.ToString(),
                repository.Private ? "private" : "public",
                repository.UpdatedAt.ToString("yyyy-MM-dd"));
        }

        console.Write(table);
    }

    internal async Task ShowStatisticsAsync()
    {
        var repository = AskRepository();
        var statistics = await LoadAsync($"Reading statistics for {repository}", () => github.GetStatisticsAsync(repository));

        var table = NewTable("Metric", "Value");
        AddRow(table, "Contributors", statistics.Contributors.ToString());
        AddRow(table, "Branches", statistics.Branches.ToString());
        AddRow(table, "Tags", statistics.Tags.ToString());
        console.Write(table);

        var total = statistics.LanguageBytes.Values.Sum();
        if (total == 0)
        {
            return;
        }

        var languages = NewTable("Language", "Bytes", "Share");
        foreach (var (language, bytes) in statistics.LanguageBytes.OrderByDescending(pair => pair.Value))
        {
            AddRow(languages, language, bytes.ToString("N0"), $"{100.0 * bytes / total:0.0}%");
        }

        console.Write(languages);
    }

    internal async Task BrowseAsync()
    {
        var repository = AskRepository();
        var path = AskOptional("Path [grey](Enter for the root)[/]:");
        var branch = AskBranch();
        var contents = await LoadAsync($"Listing {repository}/{path}", () => github.BrowseAsync(repository, path, branch));

        var table = NewTable("Type", "Name", "Size");
        foreach (var item in contents.OrderBy(item => item.Type.Value != ContentType.Dir).ThenBy(item => item.Name))
        {
            var isFile = item.Type.Value == ContentType.File;
            AddRow(table, item.Type.StringValue, item.Name, isFile ? $"{item.Size:N0} bytes" : "-");
        }

        console.Write(table);
    }

    internal async Task ShowFileAsync()
    {
        var repository = AskRepository();
        var path = console.Prompt(new TextPrompt<string>("File path:"));
        var branch = AskBranch();
        var content = await LoadAsync($"Reading {path}", () => github.GetFileContentAsync(repository, path, branch));

        if (content.Contains('\0'))
        {
            console.MarkupLineInterpolated($"{path} is a binary file; its content is not shown.");
            return;
        }

        console.Write(new Panel(new Text(content))
            .Header(Markup.Escape($"{repository}/{path}"))
            .Border(BoxBorder.Rounded));
    }

    internal async Task ListCommitsAsync()
    {
        var repository = AskRepository();
        var branch = AskBranch();
        var count = console.Prompt(new TextPrompt<int>("Number of commits [grey](1-100)[/]")
            .DefaultValue(10)
            .Validate(n => n is >= 1 and <= 100, "Enter a number from 1 to 100."));
        var commits = await LoadAsync($"Reading commits in {repository}",
            () => github.GetRecentCommitsAsync(repository, count, branch));

        var table = NewTable("SHA", "Author", "Date", "Message");
        foreach (var commit in commits)
        {
            AddRow(table,
                commit.Sha[..7],
                commit.Commit.Author.Name,
                commit.Commit.Author.Date.ToString("yyyy-MM-dd HH:mm"),
                FirstLine(commit.Commit.Message, 60));
        }

        console.Write(table);
    }

    internal async Task ShowCommitAsync()
    {
        var repository = AskRepository();
        var sha = console.Prompt(new TextPrompt<string>("Commit SHA, branch or tag:"));
        var commit = await LoadAsync($"Reading {sha}", () => github.GetCommitAsync(repository, sha));

        var table = NewTable("Field", "Value");
        AddRow(table, "SHA", commit.Sha);
        AddRow(table, "Author", $"{commit.Commit.Author.Name} <{commit.Commit.Author.Email}>");
        AddRow(table, "Date", commit.Commit.Author.Date.ToString("yyyy-MM-dd HH:mm"));
        AddRow(table, "Message", commit.Commit.Message);
        AddRow(table, "Changes", $"{Files(commit.Files.Count)}, +{commit.Stats.Additions} -{commit.Stats.Deletions}");
        AddRow(table, "Link", commit.HtmlUrl);
        console.Write(table);

        var files = NewTable("File", "Status", "Added", "Removed");
        foreach (var file in commit.Files.Take(20))
        {
            AddRow(files, file.Filename, file.Status, file.Additions.ToString(), file.Deletions.ToString());
        }

        console.Write(files);
        if (commit.Files.Count > 20)
        {
            console.MarkupLineInterpolated($"[grey]and {commit.Files.Count - 20} more files.[/]");
        }
    }

    internal async Task ListPullRequestsAsync()
    {
        var repository = AskRepository();
        var state = console.Prompt(new SelectionPrompt<ItemStateFilter>()
            .Title("Which pull requests?")
            .AddChoices(ItemStateFilter.Open, ItemStateFilter.Closed, ItemStateFilter.All));
        var pullRequests = await LoadAsync($"Reading pull requests in {repository}",
            () => github.GetPullRequestsAsync(repository, state));

        if (pullRequests.Count == 0)
        {
            console.MarkupLineInterpolated($"No {state.ToString().ToLowerInvariant()} pull requests in {repository}.");
            return;
        }

        var table = NewTable("#", "Title", "Author", "State", "Created");
        foreach (var pullRequest in pullRequests)
        {
            AddRow(table,
                pullRequest.Number.ToString(),
                pullRequest.Title,
                pullRequest.User.Login,
                pullRequest.Merged ? "merged" : pullRequest.State.StringValue,
                pullRequest.CreatedAt.ToString("yyyy-MM-dd"));
        }

        console.Write(table);
    }

    internal async Task ShowPullRequestAsync()
    {
        var repository = AskRepository();
        var number = AskPullRequestNumber();
        var pullRequest = await LoadAsync($"Reading pull request #{number}", () => github.GetPullRequestAsync(repository, number));

        var table = NewTable("Field", "Value");
        AddRow(table, "Number", pullRequest.Number.ToString());
        AddRow(table, "Title", pullRequest.Title);
        AddRow(table, "Author", pullRequest.User.Login);
        AddRow(table, "State", pullRequest.Merged ? "merged" : pullRequest.State.StringValue);
        AddRow(table, "Branches", $"{pullRequest.Head.Ref} -> {pullRequest.Base.Ref}");
        AddRow(table, "Created", pullRequest.CreatedAt.ToString("yyyy-MM-dd"));
        AddRow(table, "Commits", pullRequest.Commits.ToString());
        AddRow(table, "Changes", $"{Files(pullRequest.ChangedFiles)}, +{pullRequest.Additions} -{pullRequest.Deletions}");
        AddRow(table, "Comments", pullRequest.Comments.ToString());
        AddRow(table, "Link", pullRequest.HtmlUrl);
        console.Write(table);
    }

    internal async Task CommentOnPullRequestAsync()
    {
        var repository = AskRepository();
        var number = AskPullRequestNumber();
        var comment = console.Prompt(new TextPrompt<string>("Comment:"));
        if (!console.Confirm($"Post this comment on {Markup.Escape(repository.ToString())}#{number}?", defaultValue: false))
        {
            console.MarkupLine("Nothing was posted.");
            return;
        }

        var posted = await LoadAsync("Posting the comment", () => github.CommentOnPullRequestAsync(repository, number, comment));
        console.MarkupLineInterpolated($"[green]Comment posted:[/] {posted.HtmlUrl}");
    }

    internal async Task ReviewPullRequestAsync()
    {
        var repository = AskRepository();
        var number = AskPullRequestNumber();
        var reviewEvent = console.Prompt(new SelectionPrompt<PullRequestReviewEvent>()
            .Title("Review action:")
            .AddChoices(PullRequestReviewEvent.Comment, PullRequestReviewEvent.Approve, PullRequestReviewEvent.RequestChanges));

        // GitHub requires a body for comments and change requests; an approval may have none.
        var body = reviewEvent == PullRequestReviewEvent.Approve
            ? AskOptional("Review text [grey](optional)[/]:")
            : console.Prompt(new TextPrompt<string>("Review text:"));
        if (!console.Confirm($"Submit this review ({reviewEvent}) on {Markup.Escape(repository.ToString())}#{number}?", defaultValue: false))
        {
            console.MarkupLine("Nothing was submitted.");
            return;
        }

        var review = await LoadAsync("Submitting the review",
            () => github.ReviewPullRequestAsync(repository, number, reviewEvent, body));
        console.MarkupLineInterpolated($"[green]Review submitted:[/] {review.HtmlUrl}");
    }

    internal async Task SearchCodeAsync()
    {
        var query = console.Prompt(new TextPrompt<string>("Search for:"));
        var repository = AskOptionalRepository();
        var result = await LoadAsync("Searching code", () => github.SearchCodeAsync(query, repository));
        console.MarkupLineInterpolated($"{result.TotalCount:N0} matches; the first {result.Items.Count} are shown.");

        var table = NewTable("Repository", "Path");
        foreach (var item in result.Items)
        {
            AddRow(table, item.Repository.FullName, item.Path);
        }

        console.Write(table);
    }

    internal async Task SearchRepositoriesAsync()
    {
        var query = console.Prompt(new TextPrompt<string>("Search for [grey](for example: octokit language:csharp)[/]:"));
        var result = await LoadAsync("Searching repositories", () => github.SearchRepositoriesAsync(query));
        console.MarkupLineInterpolated($"{result.TotalCount:N0} repositories; the first {result.Items.Count} are shown.");

        var table = NewTable("Repository", "Language", "Stars", "Description");
        foreach (var repository in result.Items)
        {
            AddRow(table,
                repository.FullName,
                repository.Language ?? "-",
                repository.StargazersCount.ToString("N0"),
                FirstLine(repository.Description ?? "-", 60));
        }

        console.Write(table);
    }

    private Task<T> LoadAsync<T>(string status, Func<Task<T>> call) =>
        console.Status().StartAsync(Markup.Escape(status) + "...", _ => call());

    private RepositoryName AskRepository()
    {
        // Spectre writes the default and a colon after the question, so it ends with a colon only without one.
        var question = _lastRepository is null ? "Repository [grey](owner/name)[/]:" : "Repository [grey](owner/name)[/]";
        var prompt = new TextPrompt<string>(question)
            .Validate(text => RepositoryName.TryParse(text, out _), "Enter the repository as owner/name.");
        if (_lastRepository is not null)
        {
            prompt.DefaultValue(_lastRepository.ToString());
        }

        RepositoryName.TryParse(console.Prompt(prompt), out var repository);
        _lastRepository = repository;
        return repository;
    }

    private RepositoryName? AskOptionalRepository()
    {
        var text = console.Prompt(new TextPrompt<string>("Limit to a repository [grey](owner/name, Enter for all)[/]:")
            .AllowEmpty()
            .Validate(text => string.IsNullOrWhiteSpace(text) || RepositoryName.TryParse(text, out _),
                "Enter the repository as owner/name, or nothing."));
        return RepositoryName.TryParse(text, out var repository) ? repository : null;
    }

    private string? AskBranch() => AskOptional("Branch [grey](Enter for the default branch)[/]:");

    private int AskPullRequestNumber() =>
        console.Prompt(new TextPrompt<int>("Pull request number:").Validate(n => n > 0, "Enter a positive number."));

    private string? AskOptional(string question)
    {
        var answer = console.Prompt(new TextPrompt<string>(question).AllowEmpty());
        return string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
    }

    private static Table NewTable(params string[] columns) =>
        new Table().Border(TableBorder.Rounded).AddColumns(columns);

    private static void AddRow(Table table, params string?[] cells) =>
        table.AddRow(cells.Select(cell => (IRenderable)new Text(cell ?? string.Empty)).ToArray());

    private static string Describe(RateLimit limit) =>
        $"{limit.Remaining} of {limit.Limit}, resets at {limit.Reset.ToLocalTime():HH:mm}";

    private static string Files(int count) => count == 1 ? "1 file" : $"{count} files";

    private static string FirstLine(string text, int maxLength)
    {
        var line = text.Split('\n')[0].TrimEnd('\r');
        return line.Length > maxLength ? line[..(maxLength - 3)] + "..." : line;
    }
}
