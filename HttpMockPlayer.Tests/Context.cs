using System;
using System.IO;
using System.Reflection;

namespace HttpMockPlayer.Tests
{
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
    }
}
