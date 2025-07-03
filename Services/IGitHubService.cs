using Octokit;

namespace GitHubIntegration.Services;

public interface IGitHubService
{
    Task<User> GetCurrentUserAsync();
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync();
    Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(string owner, string repo, int count = 10);
    Task<GitHubCommit> GetCommitDetailsAsync(string owner, string repo, string sha);
    Task<string> GetFileContentAsync(string owner, string repo, string path, string? branch = null);
    Task<bool> TestConnectionAsync();
}