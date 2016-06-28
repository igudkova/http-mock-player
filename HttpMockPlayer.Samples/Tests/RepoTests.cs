using System.Threading.Tasks;
using System.Net.Http;
using NUnit.Framework;

namespace HttpMockPlayer.Samples.Tests
{
    [TestFixture]
    public class RepoTests
    {
        Cassette cassette;
        GithubClient client;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            cassette = new Cassette($"{Context.AssemblyDirectoryName}/../../Tests/Mock/Http/Repo.json");

            Context.Player.Load(cassette);

            client = new GithubClient(Context.Player.BaseAddress);
        }

        // parameterized SetUp attribute is not supported by NUnit
        public void SetUp(string record)
        {
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
            SetUp("GetRepo_ReturnsRepo");

            var repo = await client.GetRepo("igudkova", "http-mock-player");

            Assert.IsNotNull(repo);
            Assert.AreEqual("http-mock-player", repo.Name);
        }

        [Test]
        public void GetRepo_WrongParams_Throws()
        {
            SetUp("GetRepo_WrongParams_Throws");

            Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetRepo("ас-пушкин", "евгений-онегин"));
            Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetRepo("igudkova", "nonexistent-repo"));
        }

        [Test]
        public async Task GetRepoLanguages_ReturnsLanguagesList()
        {
            SetUp("GetRepoLanguages_ReturnsLanguagesList");

            var languages = await client.GetRepoLanguages("igudkova", "http-mock-player");

            Assert.AreEqual(1, languages.Count);
            Assert.AreEqual("C#", languages[0]);
        }

        [Test]
        public void CreateRepo_Unauthorized_Throws()
        {
            SetUp("CreateRepo_Unauthorized_Throws");

            Assert.ThrowsAsync<HttpRequestException>(async () => await client.CreateRepo("new-repo", "my new shiny repository"));
        }
    }
}
