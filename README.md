# GitHub Integration

A .NET console application that calls the GitHub REST API through [Octokit.net](https://github.com/octokit/octokit.net), the official GitHub client library for .NET. It is meant as a worked example: each menu action makes one or a few API calls and shows what comes back, so you can see how the API behaves on real repositories and how Octokit maps onto it.

The interface is a menu built with [Spectre.Console](https://spectreconsole.net). You choose an action, answer a few questions (a repository, a branch, a pull request number) and get the result as a table.

## Requirements

- .NET 10 SDK
- A GitHub personal access token

## Getting started

```bash
git clone https://github.com/Darko-Martinovic/GitHubIntegration.git
cd GitHubIntegration
cp .env.example .env      # then put your token in .env
dotnet run --project src/GitHubIntegration
```

The application looks for `.env` in the current folder and its parents. Variables already set in the environment take precedence over the file.

| Variable | Required | Purpose |
|---|---|---|
| `GITHUB_TOKEN` | yes | The personal access token every request is sent with. |
| `GITHUB_REPO_OWNER` | no | Together with `GITHUB_REPO_NAME`, the repository the prompts offer as the default answer. |
| `GITHUB_REPO_NAME` | no | See above. |

Once you have answered a repository prompt, that repository becomes the default for the next one.

### The token

Create a token under GitHub, Settings, Developer settings, Personal access tokens.

- **Classic token:** the `repo` scope covers every action, private repositories included. `public_repo` is enough if you only work with public repositories.
- **Fine-grained token:** choose the repositories it may access and give it read access to Contents and Pull requests. Commenting on and reviewing pull requests also needs write access to Pull requests.

The application was tested with a classic token.

## What each action calls

| Menu action | REST endpoint | Octokit call |
|---|---|---|
| Show the signed-in user and rate limits | `GET /user`, `GET /rate_limit` | `User.Current()`, `RateLimit.GetRateLimits()` |
| List my repositories | `GET /user/repos` | `Repository.GetAllForCurrent()` |
| Show statistics | `GET /repos/{owner}/{repo}/contributors`, `/languages`, `/branches`, `/tags` | `Repository.GetAllContributors()`, `GetAllLanguages()`, `Branch.GetAll()`, `GetAllTags()` |
| Browse files | `GET /repos/{owner}/{repo}/contents/{path}` | `Repository.Content.GetAllContents()` or `GetAllContentsByRef()` |
| Show a file | `GET /repos/{owner}/{repo}/contents/{path}` | as above |
| List recent commits | `GET /repos/{owner}/{repo}/commits` | `Repository.Commit.GetAll()` |
| Show a commit | `GET /repos/{owner}/{repo}/commits/{ref}` | `Repository.Commit.Get()` |
| List pull requests | `GET /repos/{owner}/{repo}/pulls` | `PullRequest.GetAllForRepository()` |
| Show a pull request | `GET /repos/{owner}/{repo}/pulls/{number}` | `PullRequest.Get()` |
| Comment on a pull request | `POST /repos/{owner}/{repo}/issues/{number}/comments` | `Issue.Comment.Create()` |
| Review a pull request | `POST /repos/{owner}/{repo}/pulls/{number}/reviews` | `PullRequest.Review.Create()` |
| Search code | `GET /search/code` | `Search.SearchCode()` |
| Search repositories | `GET /search/repositories` | `Search.SearchRepo()` |

All calls are in `src/GitHubIntegration/Services/GitHubService.cs`. The two actions that write to GitHub, commenting and reviewing, ask for confirmation before they post anything.

## Things worth knowing about the API

These are the behaviours the code has to deal with, and where you can see each one.

**Pagination.** List endpoints return one page at a time (30 items by default, at most 100) and a `Link` header pointing to the next page. Octokit's `GetAll` methods follow those links until the end unless you pass `ApiOptions`, so listing the commits of a large repository without it downloads the whole history. The commit, pull request and repository lists pass `ApiOptions` with `PageCount = 1` and the page size they need. The statistics screen deliberately reads every page, because it shows totals.

**The default branch.** Not every repository calls its default branch `main`. Leaving the branch out of a request (a null `Sha` in `CommitRequest`, or the overloads without a ref) lets GitHub use whatever the default branch is.

**Pull requests are issues.** Comments in a pull request's conversation are issue comments and go through the issues endpoint. Reviews, and comments on lines of the diff, go through the pull request endpoints.

**404 for private repositories.** GitHub answers 404 Not Found, not 403, when a token can't see a private repository, so that the repository's existence is not revealed. A 404 can therefore mean a typo or a missing permission.

**Rate limits.** An authenticated user gets 5,000 core requests an hour. Search has its own, much smaller limit per minute, and code search a smaller one again. The first menu action shows what is left; `GET /rate_limit` itself does not count against the limit.

**File contents.** The contents endpoint returns a file's text base64 encoded, which Octokit decodes into `RepositoryContent.Content`, for files up to 1 MB. For a directory it returns the list of entries instead.

**Token scopes.** For a classic token, every response carries an `X-OAuth-Scopes` header listing the token's scopes. Octokit keeps the headers of the last response, and the first menu action reads the scopes from `GetLastApiInfo()`. Fine-grained tokens have no scopes, so the list is empty.

**Errors.** Octokit turns error responses into exceptions: `AuthorizationException` (401), `ForbiddenException` (403), `RateLimitExceededException`, `NotFoundException` (404) and `ApiValidationException` (422, for example when you try to approve your own pull request). The console catches them and explains each one.

## Project layout

```
src/GitHubIntegration/
  Program.cs                      Loads .env, creates the GitHubClient and starts the menu
  Models/
    GitHubSettings.cs             Settings read from the environment
    RepositoryName.cs             owner/name, parsed from the prompts
    RepositoryStatistics.cs       Totals for the statistics screen
  Services/
    IGitHubService.cs             The API calls the console uses
    GitHubService.cs              Those calls, made with Octokit
    ConsoleUIService.cs           The menu, the prompts and the tables
tests/GitHubIntegration.Tests/    xUnit tests
```

`GitHubService` takes Octokit's `IGitHubClient` interface rather than creating a client itself, so the tests can replace it.

## Building and testing

```bash
dotnet build
dotnet test
```

The tests use xUnit v3 on Microsoft.Testing.Platform, NSubstitute and Spectre.Console.Testing. Most of them replace GitHub with a substitute and check which calls are made and what the console shows. A few live tests read public repositories (`octocat/Hello-World`, `octokit/octokit.net`) through the real API; they run when `GITHUB_TOKEN` is set in the environment and are skipped otherwise. The GitHub Actions workflow runs all of them, with the job's own token.

## History

The tag `v1` marks the original version of this repository, before it was moved to .NET 10 and the bugs found in a review were fixed.

## License

MIT. See [LICENSE](LICENSE).
