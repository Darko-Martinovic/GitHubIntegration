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

    // Phase 2 - Core Features Implementation

    public async Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string owner, string repo, ItemStateFilter state = ItemStateFilter.Open)
    {
        try
        {
            var request = new PullRequestRequest
            {
                State = state
            };

            var pullRequests = await _client.PullRequest.GetAllForRepository(owner, repo, request);
            return pullRequests.Take(_settings.MaxResults).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pull requests for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<PullRequest> GetPullRequestDetailsAsync(string owner, string repo, int number)
    {
        try
        {
            return await _client.PullRequest.Get(owner, repo, number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pull request {Number} for {Owner}/{Repo}", number, owner, repo);
            throw;
        }
    }

    public async Task<RepositoryStatistics> GetRepositoryStatisticsAsync(string owner, string repo)
    {
        try
        {
            var languages = await _client.Repository.GetAllLanguages(owner, repo);
            var languagesDict = languages.ToDictionary(l => l.Name, l => l.NumberOfBytes);

            var stats = new RepositoryStatistics
            {
                Contributors = await GetRepositoryContributorsAsync(owner, repo),
                Languages = languagesDict,
                Branches = await _client.Repository.Branch.GetAll(owner, repo),
                Tags = await _client.Repository.GetAllTags(owner, repo)
            };

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get repository statistics for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<IReadOnlyList<RepositoryContributor>> GetRepositoryContributorsAsync(string owner, string repo)
    {
        try
        {
            return await _client.Repository.GetAllContributors(owner, repo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get contributors for {Owner}/{Repo}", owner, repo);
            throw;
        }
    }

    public async Task<IssueComment> AddPullRequestCommentAsync(string owner, string repo, int prNumber, string comment)
    {
        try
        {
            return await _client.Issue.Comment.Create(owner, repo, prNumber, comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add comment to PR {Number} in {Owner}/{Repo}", prNumber, owner, repo);
            throw;
        }
    }

    public async Task<PullRequestReview> CreatePullRequestReviewAsync(string owner, string repo, int prNumber, string body, PullRequestReviewEvent reviewEvent)
    {
        try
        {
            var review = new PullRequestReviewCreate
            {
                Body = body,
                Event = reviewEvent
            };
            return await _client.PullRequest.Review.Create(owner, repo, prNumber, review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create review for PR {Number} in {Owner}/{Repo}", prNumber, owner, repo);
            throw;
        }
    }

    public async Task<IReadOnlyList<RepositoryContent>> BrowseRepositoryAsync(string owner, string repo, string path, string? branch = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(branch))
            {
                return await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, branch);
            }
            return await _client.Repository.Content.GetAllContents(owner, repo, path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to browse repository {Owner}/{Repo} at {Path}", owner, repo, path);
            throw;
        }
    }

    public async Task<SearchCodeResult> SearchCodeAsync(string query, string? repo = null)
    {
        try
        {
            var request = new SearchCodeRequest(query);
            if (!string.IsNullOrEmpty(repo))
                request.Repos.Add(repo);
            return await _client.Search.SearchCode(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search code: {Query}", query);
            throw;
        }
    }

    public async Task<SearchRepositoryResult> SearchRepositoriesAsync(string query)
    {
        try
        {
            var request = new SearchRepositoriesRequest(query);
            return await _client.Search.SearchRepo(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search repositories: {Query}", query);
            throw;
        }
    }
}