using GitHubIntegration.Models;
using Octokit;

namespace GitHubIntegration.Services;

/// <summary>The GitHub REST API calls the console uses, one method per screen.</summary>
public interface IGitHubService
{
    Task<User> GetCurrentUserAsync();

    /// <summary>The OAuth scopes of the token, read from the last response. Empty for fine-grained tokens.</summary>
    IReadOnlyList<string> GetTokenScopes();

    Task<MiscellaneousRateLimit> GetRateLimitsAsync();

    Task<IReadOnlyList<Repository>> GetMyRepositoriesAsync();

    Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(RepositoryName repository, int count, string? branch);

    Task<GitHubCommit> GetCommitAsync(RepositoryName repository, string sha);

    Task<string> GetFileContentAsync(RepositoryName repository, string path, string? branch);

    Task<IReadOnlyList<RepositoryContent>> BrowseAsync(RepositoryName repository, string? path, string? branch);

    Task<RepositoryStatistics> GetStatisticsAsync(RepositoryName repository);

    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(RepositoryName repository, ItemStateFilter state);

    Task<PullRequest> GetPullRequestAsync(RepositoryName repository, int number);

    Task<IssueComment> CommentOnPullRequestAsync(RepositoryName repository, int number, string comment);

    Task<PullRequestReview> ReviewPullRequestAsync(
        RepositoryName repository, int number, PullRequestReviewEvent reviewEvent, string? body);

    Task<SearchCodeResult> SearchCodeAsync(string query, RepositoryName? repository);

    Task<SearchRepositoryResult> SearchRepositoriesAsync(string query);
}
