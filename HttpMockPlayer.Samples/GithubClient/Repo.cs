using System;
using Newtonsoft.Json;

namespace HttpMockPlayer.Samples.GithubClient
{
    public class Repo
    {
        internal Repo() { }

        [JsonProperty("id")]
        public string Id { get; internal set; }

        [JsonProperty("name")]
        public string Name { get; internal set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; internal set; }
    }
}
