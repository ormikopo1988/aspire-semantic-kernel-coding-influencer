using Microsoft.Net.Http.Headers;
using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;
using System.Net.Http;
using System.Net;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ChatApp.Infrastructure.Plugins.GitHubPlugin
{
    public class GitHubPlugin 
    {
        private readonly ILogger<GitHubPlugin> _logger;
        
        public GitHubPlugin(ILogger<GitHubPlugin> logger)
        {
            _logger = logger;
        }

        [KernelFunction, Description("Calls the GitHub API to fetch information available for a specified username. This method should be used only if the request contains a GitHub username inside quotes.")]
        public async Task<GitHubUserDetails?> FetchUserInformationFromGithub(
            [Description("The GitHub username")] string username)
        {
            _logger.LogTrace($"Fetch information from GitHub for user: {username}");

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.github.com")
            };

            httpClient.DefaultRequestHeaders.Add(
                HeaderNames.Accept, "application/vnd.github.v3+json");
            httpClient.DefaultRequestHeaders.Add(
                HeaderNames.UserAgent, $"Semantic-Kernel-Samples-{Environment.MachineName}");

            var userDetailsResponse = await httpClient.GetAsync($"/users/{username}");

            if (userDetailsResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                var responseBody = await userDetailsResponse.Content.ReadFromJsonAsync<JsonObject>();

                var message = responseBody!["message"]!.ToString();
                
                throw new HttpRequestException(message);
            }

            userDetailsResponse.EnsureSuccessStatusCode();

            var gitHubUserDetails = await userDetailsResponse.Content.ReadFromJsonAsync<GitHubUserDetails>()!;

            var userReposResponse = await httpClient.GetAsync($"/users/{username}/repos");

            if (userReposResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                var responseBody = await userReposResponse.Content.ReadFromJsonAsync<JsonObject>();

                var message = responseBody!["message"]!.ToString();

                throw new HttpRequestException(message);
            }

            userReposResponse.EnsureSuccessStatusCode();

            var userRepos = await userReposResponse
                .Content
                .ReadFromJsonAsync<List<GitHubRepoDetails>>()!;

            gitHubUserDetails!.Repos = userRepos!
                .OrderByDescending(ur => ur.StargazersCount)
                .Take(userRepos!.Count > 0 && userRepos.Count < 3 ? userRepos.Count : 3)
                .ToList();

            return gitHubUserDetails;
        }
    }

    public class GitHubUserDetails
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public List<GitHubRepoDetails> Repos { get; set; } = [];
    }

    public class GitHubRepoDetails
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonPropertyName("stargazers_count")]
        public int StargazersCount { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = string.Empty;
    }
}
