namespace GitHubIntegration.Models;

/// <summary>A repository as GitHub names it: owner/name.</summary>
public sealed record RepositoryName(string Owner, string Name)
{
    public static bool TryParse(string? text, out RepositoryName repository)
    {
        repository = null!;
        var parts = (text ?? string.Empty).Trim().Split('/');
        if (parts.Length != 2 || parts.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        repository = new RepositoryName(parts[0].Trim(), parts[1].Trim());
        return true;
    }

    public override string ToString() => $"{Owner}/{Name}";
}
