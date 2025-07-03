namespace GitHubIntegration.Models;

public class GitHubSettings
{
    public string Token { get; set; } = string.Empty;
    public string UserAgent { get; set; } = "GitHubIntegrationConsole";
    public string DefaultOwner { get; set; } = string.Empty;
    public string DefaultRepo { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 50;
}