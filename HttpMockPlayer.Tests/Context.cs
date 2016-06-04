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

        public static string CassetteNew
        {
            get
            {
                var path = $"{Path}/new.json";

                File.Delete(path);

                return path;
            }
        }

        public static string Cassette1
        {
            get
            {
                var src = $"{assemblyDirectoryName}/../../Cassettes/cassette1.json";
                var dest = $"{Path}/cassette1.json";

                File.Copy(src, dest, true);

                return dest;
            }
        }

        public static string Cassette2
        {
            get
            {
                var src = $"{assemblyDirectoryName}/../../Cassettes/cassette2.json";
                var dest = $"{Path}/cassette2.json";

                File.Copy(src, dest, true);

                return dest;
            }
        }

        public static string Cassette3
        {
            get
            {
                var src = $"{assemblyDirectoryName}/../../Cassettes/cassette3.json";
                var dest = $"{Path}/cassette3.json";

                File.Copy(src, dest, true);

                return dest;
            }
        }

        public static string Cassette4
        {
            get
            {
                var src = $"{assemblyDirectoryName}/../../Cassettes/cassette4.json";
                var dest = $"{Path}/cassette4.json";

                File.Copy(src, dest, true);

                return dest;
            }
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
