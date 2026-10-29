using GitHubIntegration.Models;
using Octokit;

namespace GitHubIntegration.Services;

/// <summary>
/// Calls the GitHub REST API through Octokit. Lists ask for one page of the size they need:
/// without ApiOptions, Octokit's GetAll methods follow every "next" link and download the whole list.
/// </summary>
public sealed class GitHubService(IGitHubClient client) : IGitHubService
{
    public const int PageSize = 50;
    public const int SearchPageSize = 10;

    public Task<User> GetCurrentUserAsync() => client.User.Current();

    public IReadOnlyList<string> GetTokenScopes() =>
        client.GetLastApiInfo()?.OauthScopes ?? [];

    public Task<MiscellaneousRateLimit> GetRateLimitsAsync() => client.RateLimit.GetRateLimits();

    public Task<IReadOnlyList<Repository>> GetMyRepositoriesAsync() =>
        // GET /user/repos: what the token's user owns, collaborates on or reaches through an organisation.
        client.Repository.GetAllForCurrent(
            new RepositoryRequest { Sort = RepositorySort.Updated, Direction = SortDirection.Descending },
            OnePage(PageSize));

    public Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(RepositoryName repository, int count, string? branch) =>
        // A null Sha lets GitHub use the repository's default branch, whatever it is called.
        client.Repository.Commit.GetAll(
            repository.Owner, repository.Name, new CommitRequest { Sha = branch }, OnePage(count));

    public Task<GitHubCommit> GetCommitAsync(RepositoryName repository, string sha) =>
        client.Repository.Commit.Get(repository.Owner, repository.Name, sha);

    public async Task<string> GetFileContentAsync(RepositoryName repository, string path, string? branch)
    {
        var contents = await GetContentsAsync(repository, path, branch);
        var file = contents.Count == 1 && contents[0].Type.Value == ContentType.File ? contents[0] : null;
        if (file is null || !SamePath(file.Path, path))
        {
            throw new InvalidOperationException($"{path} is a directory. Use 'Browse a repository' to list it.");
        }

        // The contents API returns files up to 1 MB inline, base64 encoded; Octokit decodes them into Content.
        if (string.IsNullOrEmpty(file.Content) && file.Size > 0)
        {
            throw new InvalidOperationException(
                $"{path} is {file.Size:N0} bytes. The contents API returns the text of files up to 1 MB only.");
        }

        return file.Content ?? string.Empty;
    }

    public Task<IReadOnlyList<RepositoryContent>> BrowseAsync(RepositoryName repository, string? path, string? branch) =>
        GetContentsAsync(repository, path, branch);

    public async Task<RepositoryStatistics> GetStatisticsAsync(RepositoryName repository)
    {
        var (owner, name) = (repository.Owner, repository.Name);
        var contributors = client.Repository.GetAllContributors(owner, name);
        var languages = client.Repository.GetAllLanguages(owner, name);
        var branches = client.Repository.Branch.GetAll(owner, name);
        var tags = client.Repository.GetAllTags(owner, name);
        await Task.WhenAll(contributors, languages, branches, tags);

        return new RepositoryStatistics(
            (await contributors).Count,
            (await languages).ToDictionary(language => language.Name, language => language.NumberOfBytes),
            (await branches).Count,
            (await tags).Count);
    }

    public Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(RepositoryName repository, ItemStateFilter state) =>
        client.PullRequest.GetAllForRepository(
            repository.Owner, repository.Name, new PullRequestRequest { State = state }, OnePage(PageSize));

    public Task<PullRequest> GetPullRequestAsync(RepositoryName repository, int number) =>
        client.PullRequest.Get(repository.Owner, repository.Name, number);

    public Task<IssueComment> CommentOnPullRequestAsync(RepositoryName repository, int number, string comment) =>
        // A pull request is also an issue: comments on its conversation go through the issues API.
        client.Issue.Comment.Create(repository.Owner, repository.Name, number, comment);

    public Task<PullRequestReview> ReviewPullRequestAsync(
        RepositoryName repository, int number, PullRequestReviewEvent reviewEvent, string? body) =>
        client.PullRequest.Review.Create(
            repository.Owner, repository.Name, number, new PullRequestReviewCreate { Event = reviewEvent, Body = body });

    public Task<SearchCodeResult> SearchCodeAsync(string query, RepositoryName? repository)
    {
        var request = new SearchCodeRequest(query) { PerPage = SearchPageSize };
        if (repository is not null)
        {
            request.Repos.Add(repository.Owner, repository.Name);
        }

        return client.Search.SearchCode(request);
    }

    public Task<SearchRepositoryResult> SearchRepositoriesAsync(string query) =>
        client.Search.SearchRepo(new SearchRepositoriesRequest(query) { PerPage = SearchPageSize });

    private Task<IReadOnlyList<RepositoryContent>> GetContentsAsync(RepositoryName repository, string? path, string? branch)
    {
        var (owner, name) = (repository.Owner, repository.Name);
        var contents = client.Repository.Content;

        // Octokit has separate overloads for the root (no path) and for a ref (a branch, tag or commit).
        return (string.IsNullOrWhiteSpace(path), branch) switch
        {
            (true, null) => contents.GetAllContents(owner, name),
            (true, _) => contents.GetAllContentsByRef(owner, name, branch),
            (false, null) => contents.GetAllContents(owner, name, path!),
            (false, _) => contents.GetAllContentsByRef(owner, name, path!, branch),
        };
    }

    private static ApiOptions OnePage(int size) => new() { PageSize = size, PageCount = 1, StartPage = 1 };

    private static bool SamePath(string a, string b) =>
        string.Equals(a.Trim('/'), b.Trim('/'), StringComparison.Ordinal);
}
