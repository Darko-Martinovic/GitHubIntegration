namespace GitHubIntegration.Models;

/// <summary>Counts gathered from four API calls: contributors, languages, branches and tags.</summary>
public sealed record RepositoryStatistics(
    int Contributors,
    IReadOnlyDictionary<string, long> LanguageBytes,
    int Branches,
    int Tags);
