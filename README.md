# 🐙 GitHub Integration Console Application

A powerful .NET console application that provides seamless integration with GitHub's API using the **Octokit library**, offering a beautiful and interactive interface for managing repositories, commits, pull requests, and advanced GitHub operations.

## ✨ Features

### 🔐 Authentication & User Info

- Test GitHub API connection with personal access token
- Display comprehensive user profile information
- Validate permissions and token validity

### 📂 Repository Management

- List all accessible repositories (owned + collaborator)
- Show repository metadata (language, stars, forks, privacy status)
- Filter and sort repository information

### 📋 Commit Operations

- View recent commits from any repository
- Display commit details with author information
- Navigate through commit history with pagination

### 📝 File Operations

- Retrieve file content from any repository
- Support for different branches
- Display file content in a formatted panel

### 🔀 Pull Request Management

- List pull requests with state filtering (open, closed, all)
- View detailed pull request information
- Add comments to pull requests
- Create pull request reviews (approve, request changes, comment)

### 📊 Repository Intelligence

- View repository statistics (contributors, languages, branches, tags)
- Browse repository file structure
- Navigate directories and files with tree view

### 🔍 Search & Discovery

- Search code across repositories
- Search repositories by query
- Advanced search with filters and operators

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK or later
- GitHub Personal Access Token
- Git (for cloning the repository)

### Installation

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd GitHubIntegration
   ```

2. **Configure environment variables**

   Copy the example environment file:

   ```bash
   cp .env.example .env
   ```

   Edit `.env` and add your GitHub credentials:

   ```env
   GITHUB_TOKEN=ghp_your_personal_access_token_here
   GITHUB_REPO_OWNER=your_github_username
   GITHUB_REPO_NAME=your_default_repo_name
   ```

3. **Install dependencies**

   ```bash
   dotnet restore
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

## 🔧 Configuration

### GitHub Personal Access Token Setup

1. Go to GitHub → Settings → Developer settings → Personal access tokens
2. Generate a new token with the following scopes:
   - `repo` - Full repository access
   - `read:user` - Read user profile
   - `read:org` - Read organization membership (optional)

### Environment Variables

| Variable            | Description                       | Required |
| ------------------- | --------------------------------- | -------- |
| `GITHUB_TOKEN`      | Your GitHub Personal Access Token | ✅       |
| `GITHUB_REPO_OWNER` | Your GitHub username              | ✅       |
| `GITHUB_REPO_NAME`  | Default repository name           | ❌       |

## 🎯 Usage

### Main Menu Navigation

The application provides an intuitive console interface with submenu navigation:

#### Main Menu

1. **🔐 Test Connection & Show User Info** - Validate your GitHub connection and display profile
2. **📂 List Accessible Repositories** - Browse all repositories you have access to
3. **📋 List Recent Commits** - View recent commits from any repository
4. **📝 Get File Content** - Retrieve and display file contents
5. **🧾 Commit Details** - View detailed commit information
6. **🔀 List Pull Requests** - View pull requests with filtering
7. **👀 Get Pull Request Details** - View detailed PR information
8. **📊 Repository Statistics** - View repository analytics
9. **🔧 PR Management** - Add comments and create reviews
10. **🔍 Search & Discovery** - Search code and repositories
11. **📁 Repository Browser** - Browse file structure
12. **❌ Exit** - Close the application

#### Submenus

- **PR Management**: Add PR comments, Create PR reviews
- **Search & Discovery**: Code search, Repository search
- **Repository Browser**: Browse repository structure

### Example Interactions

#### Testing Connection

```
🔐 Test Connection & Show User Info
✅ Connection successful!

┌─────────────┬─────────────────┐
│ Property    │ Value           │
├─────────────┼─────────────────┤
│ Username    │ your-username   │
│ Name        │ Your Full Name  │
│ Email       │ your@email.com  │
│ Location    │ Your City       │
│ Public Repos│ 25              │
│ Followers   │ 150             │
│ Following   │ 75              │
│ Created     │ 2020-01-15      │
└─────────────┴─────────────────┘
```

#### Repository Listing

```
📂 Found 15 repositories:

┌─────────────────────┬──────────┬──────┬──────┬─────────┬────────────┐
│ Name                │ Language │ Stars│ Forks│ Private │ Updated    │
├─────────────────────┼──────────┼──────┼──────┼─────────┼────────────┤
│ your-username/repo1 │ C#       │ 45   │ 12   │ 🌐      │ 2024-01-15 │
│ your-username/repo2 │ Python   │ 23   │ 8    │ 🔒      │ 2024-01-10 │
└─────────────────────┴──────────┴──────┴──────┴─────────┴────────────┘
```

## 🏗️ Architecture

### Project Structure

```
GitHubIntegration/
├── Models/
│   ├── GitHubSettings.cs          # Configuration model
│   └── RepositoryStatistics.cs    # Repository statistics model
├── Services/
│   ├── IGitHubService.cs          # GitHub service interface
│   ├── GitHubService.cs           # GitHub API implementation using Octokit
│   ├── ConsoleUIService.cs        # Console UI and interactions
│   └── ConfigurationService.cs    # Configuration management
├── Program.cs                     # Application entry point
└── GitHubIntegration.csproj       # Project configuration
```

### Key Components

- **GitHubService**: Handles all GitHub API interactions using Octokit
- **ConsoleUIService**: Manages the beautiful console interface using Spectre.Console
- **ConfigurationService**: Loads and validates environment variables
- **Dependency Injection**: Properly configured services for testability

## 🛠️ Development

### Building from Source

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Code Style

The project follows C# coding conventions and uses nullable reference types.

## 📦 Dependencies

- **[Octokit](https://github.com/octokit/octokit.net)**: Official GitHub API client library for .NET
- **Spectre.Console**: Beautiful console UI components
- **Microsoft.Extensions.Configuration**: Configuration management
- **Microsoft.Extensions.DependencyInjection**: Dependency injection container
- **Microsoft.Extensions.Logging**: Structured logging
- **DotNetEnv**: Environment variable loading from .env files
- Repository browsing and navigation
- Advanced PR operations

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Troubleshooting

### Common Issues

**"GITHUB_TOKEN environment variable is required"**

- Ensure your `.env` file exists and contains the correct token
- Verify the token has the required scopes

**"Connection failed"**

- Check your internet connection
- Verify your GitHub token is valid and not expired
- Ensure the token has the correct permissions

**"Repository not found"**

- Verify the repository owner and name are correct
- Ensure you have access to the repository
- Check if the repository is private and your token has access

## 📞 Support

For issues and questions:

1. Check the troubleshooting section above
2. Review GitHub API documentation
3. Open an issue in the repository

---

**Happy coding! 🚀**
