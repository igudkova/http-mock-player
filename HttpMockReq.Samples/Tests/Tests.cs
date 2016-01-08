using NUnit.Framework;

namespace HttpMockReq.Samples
{
    [SetUp]
    public class Tests
    {
        public static Player Player;

        [SetUp]
        public static void SetUp()
        {
            Player = new Player()
            {
                BaseAddress = new System.Uri("http://localhost:5555"),
                RemoteAddress = new System.Uri("https://api.github.com")
            };
            Player.Start();
        }

        [TearDown]
        public void TearDown()
        {
            Player.Close();
        }
    }
}
