using System.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace HttpMockReq.Samples
{
    [TestFixture]
    class RepoTests
    {
        Cassette cassette;
        GithubClient client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            cassette = new Cassette($"{Tests.AssemblyDirectoryName}/../../Tests/Mock/Http/Repo.json");

            Tests.Player.Load(cassette);

            client = new GithubClient(Tests.Player.BaseAddress);
        }

        public void SetUp(string recordName)
        {
            if (cassette.Contains(recordName))
            {
                Tests.Player.Play(recordName);
            }
            else
            {
                Tests.Player.Record(recordName);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Tests.Player.Stop();
        }

        [Test, Description("Successfully retrieves list of repos")]
        public async Task CanGetRepos()
        {
            SetUp("GetRepos");

            var repos = await client.GetRepos("igudkova");
            Assert.IsNotEmpty(repos);
        }
    }
}
