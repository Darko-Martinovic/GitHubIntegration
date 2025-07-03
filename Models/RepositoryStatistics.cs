using Octokit;

namespace GitHubIntegration.Models;

public class RepositoryStatistics
{
    public IReadOnlyList<RepositoryContributor> Contributors { get; set; } = new List<RepositoryContributor>();
    public IReadOnlyDictionary<string, long> Languages { get; set; } = new Dictionary<string, long>();
    public IReadOnlyList<Branch> Branches { get; set; } = new List<Branch>();
    public IReadOnlyList<RepositoryTag> Tags { get; set; } = new List<RepositoryTag>();
}