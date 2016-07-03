using System.Net.Http;
using System.Threading.Tasks;
using NUnit.Framework;

namespace HttpMockPlayer.Samples.Tests
{
    [TestFixture]
    public class ActivityTests
    {
        Cassette cassette;
        GithubClient client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            cassette = new Cassette($"{Context.AssemblyDirectoryName}/../../Tests/Mock/Http/Activity.json");

            Context.Player.Load(cassette);

            client = new GithubClient(Context.Player.BaseAddress);
        }

        [SetUp]
        public void SetUp()
        {
            var record = TestContext.CurrentContext.Test.Name;

            if (cassette.Contains(record))
            {
                Context.Player.Play(record);
            }
            else
            {
                Context.Player.Record(record);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Context.Player.Stop();
        }

        [Test]
        public async Task GetWatchedRepos_ReturnsReposList()
        {
            var watched = await client.GetWatchedRepos("igudkova");

            Assert.IsNotEmpty(watched);
        }

        [Test]
        public void GetWatchedRepos_WrongOwner_Throws()
        {
            Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetWatchedRepos("white space"));
        }
    }
}
