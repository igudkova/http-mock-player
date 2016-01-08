using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpMockReq.Samples
{
    /// <summary>
    /// Implements sample GitHub API client.
    /// <see cref="https://developer.github.com/v3/"/>
    /// </summary>
    public class GithubClient
    {
        private HttpClient httpClient;

        public GithubClient(Uri baseUri)
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = baseUri;
        }

        #region Repo

        public async Task<string> GetRepos(string owner)
        {
            HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"users/{owner}/repos"));

            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> GetRepo(string owner, string repo)
        {
            HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"repos/{owner}/{repo}"));

            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> CreateRepo(string repo, string description)
        {
            HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "user/repos"));

            return await res.Content.ReadAsStringAsync();
        }

        #endregion

        #region Commit

        public async Task<string> GetCommits(string owner, string repo)
        {
            HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "repos/" + owner + "/" + repo + "/commits"));

            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> GetCommit(string owner, string repo, string sha)
        {
            HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "repos/" + owner + "/" + repo + "/commits"));

            return await res.Content.ReadAsStringAsync();
        }

        public async Task<string> CreateCommit(string owner, string repo, string message, string tree, string parents)
        {
            HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, "repos/" + owner + "/" + repo + "/commits"));

            return await res.Content.ReadAsStringAsync();
        }

        #endregion
    }
}
