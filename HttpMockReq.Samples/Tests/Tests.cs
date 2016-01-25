using NUnit.Framework;
using System;
using System.IO;
using System.Reflection;

namespace HttpMockReq.Samples
{
    [SetUpFixture]
    public class Tests
    {
        public static Player Player;

        [OneTimeSetUp]
        public void SetUp()
        {
            Player = new Player()
            {
                BaseAddress = new Uri("http://localhost:5555"),
                RemoteAddress = new Uri("https://api.github.com")
            };
            Player.Start();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Player.Close();
        }

        public static string AssemblyDirectoryName
        {
            get
            {
                var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                var codeBaseUriBuilder = new UriBuilder(codeBase);
                var codeBasePath = Uri.UnescapeDataString(codeBaseUriBuilder.Path);

                return Path.GetDirectoryName(codeBasePath);
            }
        }
    }
}
