using Spectre.Console;
using Octokit;
using GitHubIntegration.Services;
using GitHubIntegration.Models;

namespace GitHubIntegration.Services;

public class ConsoleUIService
{
    private readonly IGitHubService _gitHubService;

    public ConsoleUIService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    public async Task ShowMainMenuAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
           

            AnsiConsole.MarkupLine("[bold blue]=== GITHUB INTEGRATION CONSOLE ===[/]");
            AnsiConsole.WriteLine();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select an option:")
                    .PageSize(15)
                    .AddChoices(new[]
                    {
                        "🔐 Test Connection & Show User Info",
                        "📂 List Accessible Repositories",
                        "📋 List Recent Commits",
                        "📝 Get File Content",
                        "🧾 Commit Details",
                        "🔀 List Pull Requests",
                        "👀 Get Pull Request Details",
                        "📊 Repository Statistics",
                        "🔧 PR Management",
                        "🔍 Search & Discovery",
                        "📁 Repository Browser",
                        "❌ Exit"
                    }));

            await HandleMenuChoiceAsync(choice);
        }
    }

    private async Task HandleMenuChoiceAsync(string choice)
    {
        try
        {
            switch (choice)
            {
                case "🔐 Test Connection & Show User Info":
                    await TestConnectionAndShowUserInfo();
                    break;
                case "📂 List Accessible Repositories":
                    await ListRepositories();
                    break;
                case "📋 List Recent Commits":
                    await ListRecentCommits();
                    break;
                case "📝 Get File Content":
                    await GetFileContent();
                    break;
                case "🧾 Commit Details":
                    await ShowCommitDetails();
                    break;
                case "🔀 List Pull Requests":
                    await ListPullRequests();
                    break;
                case "👀 Get Pull Request Details":
                    await ShowPullRequestDetails();
                    break;
                case "📊 Repository Statistics":
                    await ShowRepositoryStatistics();
                    break;
                case "🔧 PR Management":
                    await ShowPRManagementSubmenuAsync();
                    break;
                case "🔍 Search & Discovery":
                    await ShowSearchSubmenuAsync();
                    break;
                case "📁 Repository Browser":
                    await ShowRepositoryBrowserSubmenuAsync();
                    break;
                case "❌ Exit":
                    AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
                    Environment.Exit(0);
                    break;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]Press any key to continue...[/]");
        Console.ReadKey();
    }

    private async Task TestConnectionAndShowUserInfo()
    {
        await AnsiConsole
            .Status()
            .StartAsync(
                "Testing GitHub connection...",
                async ctx =>
                {
                    ctx.Status("Connecting to GitHub API...");
                    var result = await _gitHubService.TestConnectionAsync();

                    if (result)
                    {
                        ctx.Status("Getting user information...");
                        var user = await _gitHubService.GetCurrentUserAsync();

                        AnsiConsole.MarkupLine("[green]✅ Connection successful![/]");
                        AnsiConsole.WriteLine();

                        var table = new Table()
                            .Border(TableBorder.Rounded)
                            .AddColumn("Property")
                            .AddColumn("Value");

                        table.AddRow("Username", user.Login);
                        table.AddRow("Name", user.Name ?? "N/A");
                        table.AddRow("Email", user.Email ?? "N/A");
                        table.AddRow("Location", user.Location ?? "N/A");
                        table.AddRow("Public Repos", user.PublicRepos.ToString());
                        table.AddRow("Followers", user.Followers.ToString());
                        table.AddRow("Following", user.Following.ToString());
                        table.AddRow("Created", user.CreatedAt.ToString("yyyy-MM-dd"));

                        AnsiConsole.Write(table);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]❌ Connection failed![/]");
                    }
                }
            );
    }

    private async Task ListRepositories()
    {
        await AnsiConsole
            .Status()
            .StartAsync(
                "Fetching repositories...",
                async ctx =>
                {
                    ctx.Status("Getting repository list...");
                    var repositories = await _gitHubService.GetRepositoriesAsync();

                    AnsiConsole.MarkupLine($"[blue]📂 Found {repositories.Count} repositories:[/]");
                    AnsiConsole.WriteLine();

                    var table = new Table()
                        .Border(TableBorder.Rounded)
                        .AddColumn("Name")
                        .AddColumn("Language")
                        .AddColumn("Stars")
                        .AddColumn("Forks")
                        .AddColumn("Private")
                        .AddColumn("Updated");

                    foreach (var repo in repositories)
                    {
                        table.AddRow(
                            repo.FullName,
                            repo.Language ?? "N/A",
                            repo.StargazersCount.ToString(),
                            repo.ForksCount.ToString(),
                            repo.Private ? "🔒" : "🌐",
                            repo.UpdatedAt.ToString("yyyy-MM-dd")
                        );
                    }

                    AnsiConsole.Write(table);
                }
            );
    }

    private async Task ListRecentCommits()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");
        var count = AnsiConsole.Ask<int>("Number of commits to show (default 10):", 10);

        await AnsiConsole
            .Status()
            .StartAsync(
                "Fetching commits...",
                async ctx =>
                {
                    ctx.Status($"Getting recent commits from {owner}/{repo}...");
                    var commits = await _gitHubService.GetRecentCommitsAsync(owner, repo, count);

                    AnsiConsole.MarkupLine($"[blue]📋 Recent commits in {owner}/{repo}:[/]");
                    AnsiConsole.WriteLine();

                    var table = new Table()
                        .Border(TableBorder.Rounded)
                        .AddColumn("SHA")
                        .AddColumn("Author")
                        .AddColumn("Message")
                        .AddColumn("Date");

                    foreach (var commit in commits)
                    {
                        table.AddRow(
                            commit.Sha[..8],
                            commit.Commit.Author.Name,
                            commit.Commit.Message.Length > 50
                                ? commit.Commit.Message[..47] + "..."
                                : commit.Commit.Message,
                            commit.Commit.Author.Date.ToString("yyyy-MM-dd HH:mm")
                        );
                    }

                    AnsiConsole.Write(table);
                }
            );
    }

    private async Task GetFileContent()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");
        var path = AnsiConsole.Ask<string>("Enter file path:");
        var branch = AnsiConsole.Ask<string>("Enter branch (optional, press Enter for default):");

        if (string.IsNullOrWhiteSpace(branch))
            branch = null;

        await AnsiConsole
            .Status()
            .StartAsync(
                "Fetching file content...",
                async ctx =>
                {
                    ctx.Status($"Getting content of {path} from {owner}/{repo}...");
                    var content = await _gitHubService.GetFileContentAsync(
                        owner,
                        repo,
                        path,
                        branch
                    );

                    AnsiConsole.MarkupLine($"[blue]📝 File content for {path}:[/]");
                    AnsiConsole.WriteLine();

                    var panel = new Panel(content)
                        .Header($"{owner}/{repo}/{path}")
                        .Border(BoxBorder.Rounded);

                    AnsiConsole.Write(panel);
                }
            );
    }

    private async Task ShowCommitDetails()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");
        var sha = AnsiConsole.Ask<string>("Enter commit SHA:");

        await AnsiConsole.Status()
            .StartAsync("Fetching commit details...", async ctx =>
            {
                var commit = await _gitHubService.GetCommitDetailsAsync(owner, repo, sha);
                AnsiConsole.MarkupLine($"[blue]Commit Details for {sha}:[/]");
                var table = new Table().Border(TableBorder.Rounded)
                    .AddColumn("Field").AddColumn("Value");
                table.AddRow("SHA", commit.Sha);
                table.AddRow("Author", commit.Commit.Author.Name);
                table.AddRow("Date", commit.Commit.Author.Date.ToString("yyyy-MM-dd HH:mm"));
                table.AddRow("Message", commit.Commit.Message);
                table.AddRow("Files Changed", commit.Files.Count.ToString());
                table.AddRow("Additions", commit.Stats.Additions.ToString());
                table.AddRow("Deletions", commit.Stats.Deletions.ToString());
                table.AddRow("Total Changes", commit.Stats.Total.ToString());
                AnsiConsole.Write(table);
            });
    }

    private async Task ListPullRequests()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");
        var state = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select PR state:")
                .AddChoices("Open", "Closed", "All"));
        var stateFilter = state switch
        {
            "Open" => ItemStateFilter.Open,
            "Closed" => ItemStateFilter.Closed,
            _ => ItemStateFilter.All
        };

        await AnsiConsole.Status()
            .StartAsync("Fetching pull requests...", async ctx =>
            {
                var prs = await _gitHubService.GetPullRequestsAsync(owner, repo, stateFilter);
                AnsiConsole.MarkupLine($"[blue]Pull Requests in {owner}/{repo}:[/]");
                var table = new Table().Border(TableBorder.Rounded)
                    .AddColumn("#").AddColumn("Title").AddColumn("User").AddColumn("State").AddColumn("Created");
                foreach (var pr in prs)
                {
                    table.AddRow(pr.Number.ToString(), pr.Title, pr.User.Login, pr.State.StringValue, pr.CreatedAt.ToString("yyyy-MM-dd"));
                }
                AnsiConsole.Write(table);
            });
    }

    private async Task ShowPullRequestDetails()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");
        var number = AnsiConsole.Ask<int>("Enter pull request number:");

        await AnsiConsole.Status()
            .StartAsync("Fetching pull request details...", async ctx =>
            {
                var pr = await _gitHubService.GetPullRequestDetailsAsync(owner, repo, number);
                AnsiConsole.MarkupLine($"[blue]Pull Request Details for #{number}:[/]");
                var table = new Table().Border(TableBorder.Rounded)
                    .AddColumn("Field").AddColumn("Value");
                table.AddRow("Number", pr.Number.ToString());
                table.AddRow("Title", pr.Title);
                table.AddRow("User", pr.User.Login);
                table.AddRow("State", pr.State.StringValue);
                table.AddRow("Created", pr.CreatedAt.ToString("yyyy-MM-dd"));
                table.AddRow("Merged", pr.Merged.ToString());
                table.AddRow("Comments", pr.Comments.ToString());
                table.AddRow("Commits", pr.Commits.ToString());
                table.AddRow("Additions", pr.Additions.ToString());
                table.AddRow("Deletions", pr.Deletions.ToString());
                table.AddRow("Changed Files", pr.ChangedFiles.ToString());
                AnsiConsole.Write(table);
            });
    }

    private async Task ShowRepositoryStatistics()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");

        await AnsiConsole.Status()
            .StartAsync("Fetching repository statistics...", async ctx =>
            {
                var stats = await _gitHubService.GetRepositoryStatisticsAsync(owner, repo);
                AnsiConsole.MarkupLine($"[blue]Repository Statistics for {owner}/{repo}:[/]");
                var table = new Table().Border(TableBorder.Rounded)
                    .AddColumn("Metric").AddColumn("Value");
                table.AddRow("Contributors", stats.Contributors.Count.ToString());
                table.AddRow("Languages", string.Join(", ", stats.Languages.Keys));
                table.AddRow("Branches", stats.Branches.Count.ToString());
                table.AddRow("Tags", stats.Tags.Count.ToString());
                AnsiConsole.Write(table);
            });
    }

    private async Task ShowPRManagementSubmenuAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]=== PR MANAGEMENT ===[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select PR Management option:")
                .AddChoices(new[]
                {
                    "💬 Add PR Comment",
                    "✅ Create PR Review",
                    "⬅️  Back to Main Menu"
                }));

        switch (choice)
        {
            case "💬 Add PR Comment":
                await AddPullRequestComment();
                break;
            case "✅ Create PR Review":
                await CreatePullRequestReview();
                break;
            case "⬅️  Back to Main Menu":
                return;
        }
    }

    private async Task ShowSearchSubmenuAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]=== SEARCH & DISCOVERY ===[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select search option:")
                .AddChoices(new[]
                {
                    "🔍 Search Code",
                    "📂 Search Repositories",
                    "⬅️  Back to Main Menu"
                }));

        switch (choice)
        {
            case "🔍 Search Code":
                await SearchCode();
                break;
            case "📂 Search Repositories":
                await SearchRepositories();
                break;
            case "⬅️  Back to Main Menu":
                return;
        }
    }

    private async Task ShowRepositoryBrowserSubmenuAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold blue]=== REPOSITORY BROWSER ===[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select browsing option:")
                .AddChoices(new[]
                {
                    "📁 Browse Repository Structure",
                    "⬅️  Back to Main Menu"
                }));

        switch (choice)
        {
            case "📁 Browse Repository Structure":
                await BrowseRepositoryStructure();
                break;
            case "⬅️  Back to Main Menu":
                return;
        }
    }

    // Phase 3 Implementation Methods

    private async Task AddPullRequestComment()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");
        var prNumber = AnsiConsole.Ask<int>("Enter pull request number:");
        var comment = AnsiConsole.Ask<string>("Enter your comment:");

        await AnsiConsole.Status()
            .StartAsync("Adding comment to pull request...", async ctx =>
            {
                var result = await _gitHubService.AddPullRequestCommentAsync(owner, repo, prNumber, comment);
                AnsiConsole.MarkupLine($"[green]✅ Comment added successfully![/]");
                AnsiConsole.MarkupLine($"[blue]Comment ID: {result.Id}[/]");
            });
    }

    private async Task CreatePullRequestReview()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");
        var prNumber = AnsiConsole.Ask<int>("Enter pull request number:");
        var body = AnsiConsole.Ask<string>("Enter review comment:");

        var reviewEvent = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select review action:")
                .AddChoices("Comment", "Approve", "Request Changes"));

        var eventType = reviewEvent switch
        {
            "Approve" => PullRequestReviewEvent.Approve,
            "Request Changes" => PullRequestReviewEvent.RequestChanges,
            _ => PullRequestReviewEvent.Comment
        };

        await AnsiConsole.Status()
            .StartAsync("Creating pull request review...", async ctx =>
            {
                var result = await _gitHubService.CreatePullRequestReviewAsync(owner, repo, prNumber, body, eventType);
                AnsiConsole.MarkupLine($"[green]✅ Review created successfully![/]");
                AnsiConsole.MarkupLine($"[blue]Review ID: {result.Id}[/]");
            });
    }

    private async Task SearchCode()
    {
        var query = AnsiConsole.Ask<string>("Enter search query:");
        var repo = AnsiConsole.Ask<string>("Enter repository (optional, press Enter to skip):");

        if (string.IsNullOrWhiteSpace(repo))
            repo = null;

        await AnsiConsole.Status()
            .StartAsync("Searching code...", async ctx =>
            {
                var result = await _gitHubService.SearchCodeAsync(query, repo);
                AnsiConsole.MarkupLine($"[blue]Found {result.TotalCount} code matches:[/]");
                AnsiConsole.WriteLine();

                var table = new Table().Border(TableBorder.Rounded)
                    .AddColumn("Repository").AddColumn("File").AddColumn("Language").AddColumn("Path");

                foreach (var item in result.Items.Take(10))
                {
                    table.AddRow(
                        item.Repository.FullName,
                        item.Name,
                        "N/A", // Language not available in search results
                        item.Path
                    );
                }

                AnsiConsole.Write(table);
            });
    }

    private async Task SearchRepositories()
    {
        var query = AnsiConsole.Ask<string>("Enter search query:");

        await AnsiConsole.Status()
            .StartAsync("Searching repositories...", async ctx =>
            {
                var result = await _gitHubService.SearchRepositoriesAsync(query);
                AnsiConsole.MarkupLine($"[blue]Found {result.TotalCount} repositories:[/]");
                AnsiConsole.WriteLine();

                var table = new Table().Border(TableBorder.Rounded)
                    .AddColumn("Name").AddColumn("Language").AddColumn("Stars").AddColumn("Description");

                foreach (var repo in result.Items.Take(10))
                {
                    table.AddRow(
                        repo.FullName,
                        repo.Language ?? "N/A",
                        repo.StargazersCount.ToString(),
                        repo.Description?.Length > 50 ? repo.Description[..47] + "..." : repo.Description ?? "N/A"
                    );
                }

                AnsiConsole.Write(table);
            });
    }

    private async Task BrowseRepositoryStructure()
    {
        var owner = AnsiConsole.Ask<string>("Enter repository owner:");
        var repo = AnsiConsole.Ask<string>("Enter repository name:");
        var path = AnsiConsole.Ask<string>("Enter path to browse (press Enter for root):", "");
        var branch = AnsiConsole.Ask<string>("Enter branch (optional, press Enter for default):");

        if (string.IsNullOrWhiteSpace(path))
            path = "";
        if (string.IsNullOrWhiteSpace(branch))
            branch = null;

        await AnsiConsole.Status()
            .StartAsync("Browsing repository structure...", async ctx =>
            {
                var contents = await _gitHubService.BrowseRepositoryAsync(owner, repo, path, branch);
                AnsiConsole.MarkupLine($"[blue]Repository Structure: {owner}/{repo}/{path}[/]");
                AnsiConsole.WriteLine();

                var table = new Table().Border(TableBorder.Rounded)
                    .AddColumn("Type").AddColumn("Name").AddColumn("Size").AddColumn("Path");

                foreach (var item in contents)
                {
                    var icon = item.Type == "dir" ? "📁" : "📄";
                    var size = item.Type == "dir" ? "N/A" : $"{item.Size} bytes";

                    table.AddRow(
                        icon,
                        item.Name,
                        size,
                        item.Path
                    );
                }

                AnsiConsole.Write(table);
            });
    }
}
