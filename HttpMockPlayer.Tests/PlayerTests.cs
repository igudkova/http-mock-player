using System;
using NUnit.Framework;

namespace HttpMockPlayer.Tests
{
    [TestFixture]
    public class PlayerTests
    {
        [Test]
        public void Initialize_Succeeds()
        {
            var baseAddress = new Uri("http://localhost:5555");
            var remoteAddress = new Uri("https://api.github.com");

            Player player = new Player(baseAddress, remoteAddress);

            Assert.AreSame(baseAddress, player.BaseAddress);
            Assert.AreSame(remoteAddress, player.RemoteAddress);
        }

        public void Initialize_NullArguments_Throws()
        {

        }

        public void ResetRemoteAddress_Succeeds()
        {

        }

        public void ResetRemoteAddress_NullArgument_Throws()
        {

        }

    }
}
