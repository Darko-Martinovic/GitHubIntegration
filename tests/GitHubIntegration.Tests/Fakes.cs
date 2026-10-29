using System.Text;
using Octokit;

namespace GitHubIntegration.Tests;

/// <summary>Octokit response models built with their public constructors, as GitHub would return them.</summary>
internal static class Fakes
{
    public static User User(string login) => new(
        null, null, null, 0, null, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, 0, null, 0, 0, null, null,
        0, 1, null, login, null, null, 0, null, 0, 0, 0, null, null, false, null, null);

    public static PullRequest PullRequest(int number, string title) => new(
        1, null, null, $"https://github.com/octo/repo/pull/{number}", null, null, null, null, number, ItemState.Open,
        title, null, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, null, null, Reference("feature"),
        Reference("main"), User("dana"), null, [], false, null, null, null, null, 0, 1, 2, 3, 1, null, false, null,
        [], [], [], null);

    public static RepositoryContent File(string path, string content) => new(
        path.Split('/')[^1], path, "sha", Encoding.UTF8.GetByteCount(content), ContentType.File, null, null, null,
        null, "base64", Convert.ToBase64String(Encoding.UTF8.GetBytes(content)), null, null);

    public static RepositoryContent Directory(string path) => new(
        path.Split('/')[^1], path, "sha", 0, ContentType.Dir, null, null, null, null, null, null, null, null);

    private static GitReference Reference(string name) => new(null, null, null, name, "abc1234", null, null);
}
