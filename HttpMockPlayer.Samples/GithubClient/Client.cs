using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HttpMockPlayer.Samples.GithubClient
{
    /// <summary>
    /// Implements sample GitHub API client.
    /// <see cref="https://developer.github.com/v3/"/>
    /// </summary>
    public class Client
    {
        private HttpClient httpClient;

        private async Task<T> Get<T>(string path)
        {
            var res = await httpClient.GetAsync(path);
            res.EnsureSuccessStatusCode();

            var resString = await res.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(resString);
        }

        private async Task<T> Post<T>(string path, string content)
        {
            var res = await httpClient.PostAsync(path, new StringContent(content));
            res.EnsureSuccessStatusCode();

            var resString = await res.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(resString);
        }

        public Client(Uri baseAddress)
        {
            httpClient = new HttpClient();
            httpClient.BaseAddress = baseAddress;
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SampleGithubClient", "1.0"));
        }

        #region Repo

        public async Task<Repo> GetRepo(string owner, string repo)
        {
            return await Get<Repo>($"repos/{owner}/{repo}");
        }

        public async Task<List<string>> GetRepoLanguages(string owner, string repo)
        {
            var languages = await Get<JObject>($"repos/{owner}/{repo}/languages");
            
            var list = from JProperty language in languages.Children()
                       select language.Name;

            return list.ToList();
        } 

        public async Task<Repo> CreateRepo(string name, string description)
        {
            var repo = new
            {
                name = name,
                description = description
            };

            return await Post<Repo>("/user/repos", JsonConvert.SerializeObject(repo));
        }

        #endregion

        #region Activity

        public async Task<List<Repo>> GetWatchedRepos(string owner)
        {
            return await Get<List<Repo>>($"users/{owner}/subscriptions");
        }

        #endregion
    }
}
