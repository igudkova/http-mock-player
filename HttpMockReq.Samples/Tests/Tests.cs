using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace HttpMockReq.Samples
{
    [SetUpFixture]
    public class Tests
    {
        public static Player Player;

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

        [OneTimeSetUp]
        public void SetUp()
        {
            Player = new Player(new Uri("http://localhost:5555"), new Uri("https://api.github.com"));
            Player.Start();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Player.Close();
        }
    }
}
