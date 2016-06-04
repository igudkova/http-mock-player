using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Specialized;
using NUnit.Framework;

namespace HttpMockPlayer.Tests
{
    [TestFixture]
    public class PlayerTests
    {
        Uri baseAddress = new Uri("http://localhost:5550");
        Uri remoteAddress = new Uri("https://localhost:5560");
        Player player;

        Cassette cassette1 = new Cassette(Context.Cassette1);
        Cassette cassette2 = new Cassette(Context.Cassette2);
        Cassette cassette3 = new Cassette(Context.Cassette3);

        [SetUp]
        public void SetUp()
        {
            player = new Player(baseAddress, remoteAddress);
        }

        [TearDown]
        public void TearDown()
        {
            player.Close();
        }

        [Test]
        public void Initialize_SetsAddresses()
        {
            Assert.AreSame(baseAddress, player.BaseAddress);
            Assert.AreSame(remoteAddress, player.RemoteAddress);
        }

        [Test]
        public void Initialize_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Player(baseAddress, null));
            Assert.Throws<ArgumentNullException>(() => new Player(null, remoteAddress));
        }

        [Test]
        public void Initialize_SetsState()
        {
            Assert.AreEqual(player.CurrentState, Player.State.Off);
        }

        [Test]
        public void SetRemoteAddress_NullAddress_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => player.RemoteAddress = null);
        }

        [Test]
        public void Play_IsOff_Throws()
        {
            player.Load(cassette1);

            var ex = Assert.Throws<PlayerStateException>(() => player.Play("record1"));

            Assert.AreEqual(ex.State, Player.State.Off);
        }

        [Test]
        public void Play_IsBusy_Throws()
        {
            player.Start();
            player.Load(cassette1);
            player.Play("record1");

            var ex = Assert.Throws<PlayerStateException>(() => player.Play("record1"));
            Assert.AreEqual(ex.State, Player.State.Playing);

            player.Stop();
            player.Record("newrecord");

            ex = Assert.Throws<PlayerStateException>(() => player.Play("record1"));
            Assert.AreEqual(ex.State, Player.State.Recording);
        }

        [Test]
        public void Play_CassetteNotLoaded_Throws()
        {
            player.Start();

            Assert.Throws<InvalidOperationException>(() => player.Play("record1"));
        }

        [Test]
        public void Play_RecordNotFound_Throws()
        {
            player.Start();
            player.Load(cassette1);

            Assert.Throws<ArgumentException>(() => player.Play("wrong"));
        }

        [Test]
        public void Play_IsIdle_SetsState()
        {
            player.Start();
            player.Load(cassette1);
            player.Play("record1");

            Assert.AreEqual(player.CurrentState, Player.State.Playing);
        }

        [Test]
        public void Record_IsOff_Throws()
        {
            player.Load(cassette1);

            var ex = Assert.Throws<PlayerStateException>(() => player.Record("newrecord"));

            Assert.AreEqual(ex.State, Player.State.Off);
        }

        [Test]
        public void Record_IsBusy_Throws()
        {
            player.Start();
            player.Load(cassette1);
            player.Play("record1");

            var ex = Assert.Throws<PlayerStateException>(() => player.Record("newrecord"));
            Assert.AreEqual(ex.State, Player.State.Playing);

            player.Stop();
            player.Record("newrecord");

            ex = Assert.Throws<PlayerStateException>(() => player.Record("newrecord"));
            Assert.AreEqual(ex.State, Player.State.Recording);
        }

        [Test]
        public void Record_CassetteNotLoaded_Throws()
        {
            player.Start();

            Assert.Throws<InvalidOperationException>(() => player.Record("newrecord"));
        }

        [Test]
        public void Record_IsIdle_SetsState()
        {
            player.Start();
            player.Load(cassette1);
            player.Record("newrecord");

            Assert.AreEqual(player.CurrentState, Player.State.Recording);
        }

        [Test]
        public void Stop_IsOff_Throws()
        {
            var ex = Assert.Throws<PlayerStateException>(() => player.Stop());

            Assert.AreEqual(ex.State, Player.State.Off);
        }

        [Test]
        public void Stop_SetsState()
        {
            player.Start();
            player.Stop();
            Assert.AreEqual(player.CurrentState, Player.State.Idle);

            player.Load(cassette1);
            player.Play("record1");
            player.Stop();
            Assert.AreEqual(player.CurrentState, Player.State.Idle);

            player.Record("newrecord");
            player.Stop();
            Assert.AreEqual(player.CurrentState, Player.State.Idle);
        }

        [Test]
        public void Start_IsOn_Throws()
        {
            player.Start();

            var ex = Assert.Throws<PlayerStateException>(() => player.Start());
            Assert.AreEqual(ex.State, Player.State.Idle);

            player.Load(cassette1);
            player.Play("record1");

            ex = Assert.Throws<PlayerStateException>(() => player.Start());
            Assert.AreEqual(ex.State, Player.State.Playing);

            player.Stop();

            ex = Assert.Throws<PlayerStateException>(() => player.Start());
            Assert.AreEqual(ex.State, Player.State.Idle);

            player.Record("newrecord");

            ex = Assert.Throws<PlayerStateException>(() => player.Start());
            Assert.AreEqual(ex.State, Player.State.Recording);
        }

        [Test]
        public void Start_SetsState()
        {
            player.Start();

            Assert.AreEqual(player.CurrentState, Player.State.Idle);
        }

        [Test]
        public void Start_AcceptsRequests()
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                TestDelegate connectTest = () => socket.Connect(baseAddress.Host, baseAddress.Port);

                SocketException ex = Assert.Throws<SocketException>(connectTest);

                Assert.AreEqual(ex.SocketErrorCode, SocketError.ConnectionRefused);

                player.Start();

                Assert.DoesNotThrow(connectTest);
            }
        }

        [Test]
        public void Load_HasStarted_CanChangeCassette()
        {
            var client = new Client(baseAddress);

            player.Start();
            player.Load(cassette1);
            player.Play("record1");

            using (var response = (HttpWebResponse)client.Send("/", "GET"))
            {
                Assert.AreEqual(response.Headers["X-Request-Id"], "request1_record1_cassette1");
            }

            player.Stop();
            player.Load(cassette2);
            player.Play("record1");

            using (var response = (HttpWebResponse)client.Send("/", "GET"))
            {
                Assert.AreEqual(response.Headers["X-Request-Id"], "request1_record1_cassette2");
            }
        }

        [Test]
        public void Idle_IncomingRequest_ResponsesWithPlayerError()
        {
            player.Start();

            var client = new Client(baseAddress);
            var ex = Assert.Throws<WebException>(() => client.Send("/anypath", "GET"));

            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual((int)response.StatusCode, 550);
                Assert.AreEqual(response.StatusDescription, "Player exception");
            }
        }

        [Test]
        public void Playing_PlaysOrderedRecordRequests()
        {
            var client = new Client(baseAddress);

            player.Start();
            player.Load(cassette1);
            player.Play("record1");

            using (var response = (HttpWebResponse)client.Send("/", "GET"))
            {
                Assert.AreEqual(response.Headers["X-Request-Id"], "request1_record1_cassette1");
            }

            var ex = Assert.Throws<WebException>(() => client.Send("/wrong/path", "GET"));
            using (var response = ex.Response)
            {
                Assert.AreEqual(response.Headers["X-Request-Id"], "request2_record1_cassette1");
            }

            using (var response = (HttpWebResponse)client.Send("/request3", "GET"))
            {
                Assert.AreEqual(response.Headers["X-Request-Id"], "request3_record1_cassette1");
            }
        }

        [Test]
        public void Playing_ResponsesWithRecordedResponse()
        {
            //response has all same props as in json
        }

        [Test]
        public void Playing_RequestMismatch_ResponsesWithPlayerError()
        {
            var client = new Client(baseAddress);

            player.Start();
            player.Load(cassette3);

            player.Play("record1");

            // wrong path
            var ex = Assert.Throws<WebException>(() => client.Send("/wrong", "GET"));
            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual((int)response.StatusCode, 454);
                Assert.AreEqual(response.StatusDescription, "Player request mismatch");
            }

            player.Stop();
            player.Play("record1");

            // wrong method
            ex = Assert.Throws<WebException>(() => client.Send("/request1", "POST", "content"));
            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual((int)response.StatusCode, 454);
            }

            // wrong content
            ex = Assert.Throws<WebException>(() => client.Send("/request2", "POST", "dålig"));
            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual((int)response.StatusCode, 454);
            }

            // mismatching headers list
            ex = Assert.Throws<WebException>(() => client.Send("/request3", "GET", headers: new NameValueCollection {{"Custom-Header", "wrong"}}));
            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual((int)response.StatusCode, 454);
            }

            // mismatching cookies collection
            var cookies = new CookieCollection();
            cookies.Add(new Cookie() {
                Name = "cookie1",
                Value = "wrong",
                Domain = baseAddress.Host
            });

            ex = Assert.Throws<WebException>(() => client.Send("/request4", "GET", cookies: cookies));
            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual((int)response.StatusCode, 454);
            }
        }

        [Test]
        public void Playing_Exception_ResponsesWithPlayerError()
        {
            var client = new Client(baseAddress);

            player.Start();
            player.Load(cassette3);
            player.Play("badrecord");

            var ex = Assert.Throws<WebException>(() => client.Send("/", "GET"));

            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual((int)response.StatusCode, 551);
                Assert.AreEqual(response.StatusDescription, "Player exception");
            }
        }

        [Test]
        public void Recording_RedirectsRequestToRemoteAddress()
        {
            //remote srv receives same request as did the player
        }

        [Test]
        public void Recording_CanHandleMultipleRemoteAddresses()
        {

        }

        [Test]
        public void Recording_ResponsesWithRemoteResponse()
        {
            //response has all same props as remote srv response
        }

        [Test]
        public void Recording_WebException_ResponsesWithWebExceptionResponse()
        {

        }

        [Test]
        public void Recording_Exception_ResponsesWithPlayerError()
        {
            //response has status 552
        }

        [Test]
        public void Stop_AfterPlayed_RewindsRecord()
        {
            //can play twice
        }

        [Test]
        public void Stop_AfterRecorded_RewindsRecord()
        {
            //can play after recorded
        }

        [Test]
        public void Stop_AfterRecorded_SavesRecord()
        {
            //record json has all same props as accepted request and remote response
        }

        [Test]
        public void Close_NotAcceptsRequests()
        {
            player.Start();
            player.Close();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                var ex = Assert.Throws<SocketException>(() => socket.Connect(baseAddress.Host, baseAddress.Port));

                Assert.AreEqual(ex.SocketErrorCode, SocketError.ConnectionRefused);
            }
        }

        [Test]
        public void Close_SetsState()
        {
            player.Start();
            player.Close();

            Assert.AreEqual(player.CurrentState, Player.State.Off);
        }
    }
}
