using Spectre.Console;
using Octokit;
using GitHubIntegration.Services;

namespace GitHubIntegration.Services;

public class ConsoleUIService
{
    private readonly IGitHubService _gitHubService;

    public ConsoleUIService(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
    }

    public void ShowMainMenu()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new FigletText("GitHub Integration").Centered().Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold blue]=== GITHUB INTEGRATION CONSOLE ===[/]");
        AnsiConsole.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an option:")
                .PageSize(15)
                .AddChoices(
                    new[]
                    {
                        "🔐 Test Connection & Show User Info",
                        "📂 List Accessible Repositories",
                        "📋 List Recent Commits",
                        "📝 Get File Content",
                        "❌ Exit"
                    }
                )
        );

        HandleMenuChoice(choice);
    }

    private void HandleMenuChoice(string choice)
    {
        try
        {
            switch (choice)
            {
                case "🔐 Test Connection & Show User Info":
                    TestConnectionAndShowUserInfo().Wait();
                    break;
                case "📂 List Accessible Repositories":
                    ListRepositories().Wait();
                    break;
                case "📋 List Recent Commits":
                    ListRecentCommits().Wait();
                    break;
                case "📝 Get File Content":
                    GetFileContent().Wait();
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
        ShowMainMenu();
    }

    private async Task TestConnectionAndShowUserInfo()
    {
        AnsiConsole
            .Status()
            .Start(
                "Testing GitHub connection...",
                ctx =>
                {
                    ctx.Status("Connecting to GitHub API...");
                    var result = _gitHubService.TestConnectionAsync().Result;

                    if (result)
                    {
                        ctx.Status("Getting user information...");
                        var user = _gitHubService.GetCurrentUserAsync().Result;

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
        AnsiConsole
            .Status()
            .Start(
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

        AnsiConsole
            .Status()
            .Start(
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

        AnsiConsole
            .Status()
            .Start(
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
}
