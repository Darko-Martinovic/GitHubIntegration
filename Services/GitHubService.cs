using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using GitHubIntegration.Models;

namespace GitHubIntegration.Services;

public class GitHubService : IGitHubService
{
    private readonly GitHubClient _client;
    private readonly ILogger<GitHubService> _logger;
    private readonly GitHubSettings _settings;

    public GitHubService(GitHubSettings settings, ILogger<GitHubService> logger)
    {
        _settings = settings;
        _logger = logger;

        _client = new GitHubClient(new ProductHeaderValue(_settings.UserAgent))
        {
            Credentials = new Credentials(_settings.Token)
        };
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var user = await _client.User.Current();
            _logger.LogInformation("Successfully connected to GitHub as {Username}", user.Login);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to GitHub API");
            return false;
        }
    }

    public async Task<User> GetCurrentUserAsync()
    {
        try
        {
            return await _client.User.Current();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
            throw;
        }
    }

    public async Task<IReadOnlyList<Repository>> GetRepositoriesAsync()
    {
        try
        {
            var request = new SearchRepositoriesRequest
            {
                User = _settings.DefaultOwner,
                PerPage = _settings.MaxResults
            };

            var searchResult = await _client.Search.SearchRepo(request);
            return searchResult.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get repositories");
            throw;
        }
    }

    public async Task<IReadOnlyList<GitHubCommit>> GetRecentCommitsAsync(string owner, string repo, int count = 10)
    {
        try
        {
            var commits = await _client.Repository.Commit.GetAll(owner, repo, new CommitRequest
            {
                Sha = "main" // Default to main branch
            });

            return commits.Take(count).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent commits for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<GitHubCommit> GetCommitDetailsAsync(string owner, string repo, string sha)
    {
        try
        {
            return await _client.Repository.Commit.Get(owner, repo, sha);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get commit details for {Sha} in {Owner}/{Repo}", sha, owner, repo);
            throw;
        }
    }

    public async Task<string> GetFileContentAsync(string owner, string repo, string path, string? branch = null)
    {
        try
        {
            var content = await _client.Repository.Content.GetAllContents(owner, repo, path);
            return content.FirstOrDefault()?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file content for {Path} in {Owner}/{Repo}", path, owner, repo);
            throw;
        }
    }
}