using System.Threading.Tasks;
using NUnit.Framework;

namespace HttpMockReq.Samples
{
    [TestFixture]
    class RepoTests
    {
        GithubClient client;
        Cassette cassette;

        [OneTimeSetUp]
        public void SetUp()
        {
            cassette = new Cassette($"{Tests.AssemblyDirectoryName}/../../Tests/Mock/Http/Repo.json");

            Tests.Player.Load(cassette);

            client = new GithubClient(Tests.Player.BaseAddress);
            //client = new GithubClient(new System.Uri("http://localhost:5555"));
        }

        public void PreTest(string recordName)
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

        public void PostTest()
        {
            Tests.Player.Stop();
        }

        [Test, Description("Successfully retrieves list of repos")]
        public async Task CanGetRepos()
        {
            PreTest("GetRepos");

            var repos = await client.GetRepos("igudkova");
            Assert.IsNotEmpty(repos);

            PostTest();
        }
    }
}
