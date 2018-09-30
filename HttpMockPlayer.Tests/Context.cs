using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace HttpMockPlayer.Tests
{
    [SetUpFixture]
    public class Context
    {
        private static string assemblyDirectoryName;

        public static string Path { get; private set; }

        public static string CreateCassette(string name)
        {
            var path = $"{Path}/{name}.json";

            if (name == "new")
            {
                File.Delete(path);
            }
            else
            {
                File.Copy($"{assemblyDirectoryName}/../../Cassettes/{name}.json", path, true);
            }

            return path;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var codeBaseUriBuilder = new UriBuilder(codeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUriBuilder.Path);
            assemblyDirectoryName = System.IO.Path.GetDirectoryName(codeBasePath);

            Path = $"{assemblyDirectoryName}/Cassettes";

            Directory.CreateDirectory(Path);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Directory.Delete(Path, true);
        }
    }
}
