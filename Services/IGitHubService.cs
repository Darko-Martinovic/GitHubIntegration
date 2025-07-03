using Octokit;
using GitHubIntegration.Models;

namespace GitHubIntegration.Services;

public interface IGitHubService
{
    Task<User> GetCurrentUserAsync();
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync();
    Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(string owner, string repo, int count = 10);
    Task<GitHubCommit> GetCommitDetailsAsync(string owner, string repo, string sha);
    Task<string> GetFileContentAsync(string owner, string repo, string path, string? branch = null);
    Task<bool> TestConnectionAsync();

    // Phase 2 - Core Features
    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, ItemStateFilter state = ItemStateFilter.Open);
    Task<PullRequest> GetPullRequestDetailsAsync(string owner, string repo, int number);
    Task<RepositoryStatistics> GetRepositoryStatisticsAsync(string owner, string repo);
    Task<IReadOnlyList<RepositoryContributor>> GetRepositoryContributorsAsync(string owner, string repo);

    // Phase 3
    Task<IssueComment> AddPullRequestCommentAsync(string owner, string repo, int prNumber, string comment);
    Task<PullRequestReview> CreatePullRequestReviewAsync(string owner, string repo, int prNumber, string body, PullRequestReviewEvent reviewEvent);
    Task<IReadOnlyList<RepositoryContent>> BrowseRepositoryAsync(string owner, string repo, string path, string? branch = null);
    Task<SearchCodeResult> SearchCodeAsync(string query, string? repo = null);
    Task<SearchRepositoryResult> SearchRepositoriesAsync(string query);
}