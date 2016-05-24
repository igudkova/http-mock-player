using System.Threading.Tasks;
using NUnit.Framework;

namespace HttpMockPlayer.Samples.Tests
{
    [TestFixture]
    class RepoTests
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
        public void SetUp(string recordName)
        {
            if (cassette.Contains(recordName))
            {
                Context.Player.Play(recordName);
            }
            else
            {
                Context.Player.Record(recordName);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Context.Player.Stop();
        }

        [Test, Description("Successfully retrieves list of repos")]
        public async Task GetRepos_Succeeds()
        {
            SetUp("GetRepos_Succeeds");

            var repos = await client.GetRepos("igudkova");
            Assert.IsNotEmpty(repos);
        }

        public void GetRepos_WrongOwner_ReturnsNotFound()
        {

        }

        public void GetRepo_Succeeds()
        {

        }

        public void GetRepo_WrongOwner_ReturnsNotFound()
        {

        }

        public void GetRepo_WrongRepo_ReturnsNotFound()
        {

        }

        public void CreateRepo_NotLoggedIn_ReturnsUnauthorized()
        {

        }
    }
}
