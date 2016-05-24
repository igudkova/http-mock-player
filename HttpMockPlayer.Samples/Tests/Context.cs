using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace HttpMockPlayer.Samples.Tests
{
    [SetUpFixture]
    public class Context
    {
        private static string assemblyDirectoryName;

        public static string AssemblyDirectoryName
        {
            get
            {
                if (assemblyDirectoryName == null)
                {
                    var codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    var codeBaseUriBuilder = new UriBuilder(codeBase);
                    var codeBasePath = Uri.UnescapeDataString(codeBaseUriBuilder.Path);

                    assemblyDirectoryName = Path.GetDirectoryName(codeBasePath);
                }

                return assemblyDirectoryName;
            }
        }

        public static Player Player { get; private set; }

        [OneTimeSetUp]
        public void SetUp()
        {
            var baseAddress = new Uri("http://localhost:5555");
            var remoteAddress = new Uri("https://api.github.com");

            Player = new Player(baseAddress, remoteAddress);

            Player.Start();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            Player.Close();
        }
    }
}
