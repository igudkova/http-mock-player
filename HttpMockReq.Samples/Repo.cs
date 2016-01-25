using Newtonsoft.Json;
using System;

namespace HttpMockReq.Samples
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
