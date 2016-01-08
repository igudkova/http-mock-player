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
            Tests.Player.Load(new Cassette("../../Tests/Mock/Http/Repo.json"));

            client = new GithubClient(Tests.Player.BaseAddress);
        }

        //[SetUp]
        //protected void dofirst()
        //{

        //}

        [Test]
        public async void CanGetRepos()
        {
            var record = cassette.Records.Find(r => r.Name == "GetRepos");
            if(record != null)
            {
                Tests.Player.Play(record);
            }
            else
            {
                Tests.Player.Record("GetRepos");
            }

            var repos = await client.GetRepos("igudkova");

            Tests.Player.Stop();
        }
    }
}
