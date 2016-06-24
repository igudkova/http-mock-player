using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HttpMockPlayer.Samples
{
    /// <summary>
    /// Implements sample GitHub API client.
    /// <see cref="https://developer.github.com/v3/"/>
    /// </summary>
    public class GithubClient
    {
        private HttpClient httpClient;

        public GithubClient(Uri baseAddress)
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = baseAddress;
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SampleGithubClient", "1.0"));
        }

        #region Repo

        public async Task<List<Repo>> GetRepos(string owner)
        {
            HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"/users/{owner}/repos"));
            var resString = await res.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<Repo>>(resString);
        }

        public async Task<Repo> GetRepo(string owner, string repo)
        {
            HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"repos/{owner}/{repo}"));
            var resString = await res.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<Repo>(resString);
        }

        public async Task<string> CreateRepo(string repo, string description)
        {
            var res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "user/repos"));

            return await res.Content.ReadAsStringAsync();
        }

        #endregion

        #region Commit

        public async Task<string> GetCommits(string owner, string repo)
        {
            var res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"repos/{owner}/{repo}/commits"));

            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> GetCommit(string owner, string repo, string sha)
        {
            var res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"repos/{owner}/{repo}/commits"));

            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> CreateCommit(string owner, string repo, string message, string tree, string parents)
        {
            var res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "repos/" + owner + "/" + repo + "/commits"));

            return await res.Content.ReadAsStringAsync();
        }

        #endregion
    }
}
