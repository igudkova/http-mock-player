using System.Threading.Tasks;
using System.Net.Http;
using NUnit.Framework;

namespace HttpMockPlayer.Samples.GithubClient.Tests
{
    [TestFixture]
    public class RepoTests
    {
        Cassette cassette;
        Client client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            cassette = new Cassette($"{Context.AssemblyDirectoryName}/../../GithubClient/Tests/Mock/Http/Repo.json");

            Context.Player.Load(cassette);

            client = new Client(Context.Player.BaseAddress);
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
        public async Task GetRepo_ReturnsRepo()
        {
            var repo = await client.GetRepoAsync("igudkova", "http-mock-player");

            Assert.IsNotNull(repo);
            Assert.AreEqual("http-mock-player", repo.Name);
        }

        [Test]
        public void GetRepo_WrongParams_Throws()
        {
            Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetRepoAsync("ас-пушкин", "евгений-онегин"));
            Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetRepoAsync("igudkova", "nonexistent-repo"));
        }

        [Test]
        public async Task GetRepoLanguages_ReturnsLanguagesList()
        {
            var languages = await client.GetRepoLanguagesAsync("igudkova", "http-mock-player");

            Assert.AreEqual(1, languages.Count);
            Assert.AreEqual("C#", languages[0]);
        }

        [Test]
        public void CreateRepo_Unauthorized_Throws()
        {
            Assert.ThrowsAsync<HttpRequestException>(async () => await client.CreateRepoAsync("new-repo", "my new shiny repository"));
        }
    }
}
