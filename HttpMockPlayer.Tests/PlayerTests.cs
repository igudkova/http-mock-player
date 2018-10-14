using System;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;

namespace HttpMockPlayer.Tests
{
    [TestFixture]
    public class PlayerTests
    {
        Uri baseAddress = new Uri("http://localhost:5550");
        Uri remoteAddress1 = new Uri("http://localhost:5560");
        Uri remoteAddress2 = new Uri("http://localhost:5570");

        Player player;

        Cassette cassette1 = new Cassette(Context.CreateCassette("cassette1"));
        Cassette cassette2 = new Cassette(Context.CreateCassette("cassette2"));
        Cassette cassette3 = new Cassette(Context.CreateCassette("cassette3"));

        [SetUp]
        public void SetUp()
        {
            player = new Player(baseAddress, remoteAddress1);
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
            Assert.AreSame(remoteAddress1, player.RemoteAddress);
        }

        [Test]
        public void Initialize_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Player(baseAddress, null));
            Assert.Throws<ArgumentNullException>(() => new Player(null, remoteAddress1));
        }

        [Test]
        public void Initialize_SetsState()
        {
            Assert.AreEqual(Player.State.Off, player.CurrentState);
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

            Assert.AreEqual(Player.State.Off, ex.State);
        }

        [Test]
        public void Play_IsBusy_Throws()
        {
            player.Start();
            player.Load(cassette1);
            player.Play("record1");

            var ex = Assert.Throws<PlayerStateException>(() => player.Play("record1"));
            Assert.AreEqual(Player.State.Playing, ex.State);

            player.Stop();
            player.Record("newrecord");

            ex = Assert.Throws<PlayerStateException>(() => player.Play("record1"));
            Assert.AreEqual(Player.State.Recording, ex.State);
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

            Assert.AreEqual(Player.State.Playing, player.CurrentState);
        }

        [Test]
        public void Record_IsOff_Throws()
        {
            player.Load(cassette1);

            var ex = Assert.Throws<PlayerStateException>(() => player.Record("newrecord"));

            Assert.AreEqual(Player.State.Off, ex.State);
        }

        [Test]
        public void Record_IsBusy_Throws()
        {
            player.Start();
            player.Load(cassette1);
            player.Play("record1");

            var ex = Assert.Throws<PlayerStateException>(() => player.Record("newrecord"));
            Assert.AreEqual(Player.State.Playing, ex.State);

            player.Stop();
            player.Record("newrecord");

            ex = Assert.Throws<PlayerStateException>(() => player.Record("newrecord"));
            Assert.AreEqual(Player.State.Recording, ex.State);
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

            Assert.AreEqual(Player.State.Recording, player.CurrentState);
        }

        [Test]
        public void Stop_IsOff_Throws()
        {
            var ex = Assert.Throws<PlayerStateException>(() => player.Stop());

            Assert.AreEqual(Player.State.Off, ex.State);
        }

        [Test]
        public void Stop_SetsState()
        {
            player.Start();
            player.Stop();
            Assert.AreEqual(Player.State.Idle, player.CurrentState);

            player.Load(cassette1);
            player.Play("record1");
            player.Stop();
            Assert.AreEqual(Player.State.Idle, player.CurrentState);

            player.Record("newrecord");
            player.Stop();
            Assert.AreEqual(Player.State.Idle, player.CurrentState);
        }

        [Test]
        public void Start_IsOn_Throws()
        {
            player.Start();

            var ex = Assert.Throws<PlayerStateException>(() => player.Start());
            Assert.AreEqual(Player.State.Idle, ex.State);

            player.Load(cassette1);
            player.Play("record1");

            ex = Assert.Throws<PlayerStateException>(() => player.Start());
            Assert.AreEqual(Player.State.Playing, ex.State);

            player.Stop();

            ex = Assert.Throws<PlayerStateException>(() => player.Start());
            Assert.AreEqual(Player.State.Idle, ex.State);

            player.Record("newrecord");

            ex = Assert.Throws<PlayerStateException>(() => player.Start());
            Assert.AreEqual(Player.State.Recording, ex.State);
        }

        [Test]
        public void Start_SetsState()
        {
            player.Start();

            Assert.AreEqual(Player.State.Idle, player.CurrentState);
        }

        [Test]
        public void Start_AcceptsRequests()
        {
            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                TestDelegate connectTest = () => socket.Connect(baseAddress.Host, baseAddress.Port);

                SocketException ex = Assert.Throws<SocketException>(connectTest);

                Assert.AreEqual(SocketError.ConnectionRefused, ex.SocketErrorCode);

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
                Assert.AreEqual("request1_record1_cassette1", response.Headers["X-Request-Id"]);
            }

            player.Stop();
            player.Load(cassette2);
            player.Play("record1");

            using (var response = (HttpWebResponse)client.Send("/", "GET"))
            {
                Assert.AreEqual("request1_record1_cassette2", response.Headers["X-Request-Id"]);
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
                Assert.AreEqual(550, (int)response.StatusCode);
                Assert.AreEqual("Player exception", response.StatusDescription);
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
                Assert.AreEqual("request1_record1_cassette1", response.Headers["X-Request-Id"]);
            }

            var ex = Assert.Throws<WebException>(() => client.Send("/wrong/path", "GET"));
            using (var response = ex.Response)
            {
                Assert.AreEqual("request2_record1_cassette1", response.Headers["X-Request-Id"]);
            }

            using (var response = (HttpWebResponse)client.Send("/request3", "GET"))
            {
                Assert.AreEqual("request3_record1_cassette1", response.Headers["X-Request-Id"]);
            }
        }

        [Test]
        public void Playing_ResponsesWithRecordedResponse()
        {
            var client = new Client(baseAddress);

            player.Start();
            player.Load(cassette3);

            player.Play("record1");

            using (var response = (HttpWebResponse)client.Send("/request1", "GET", cookies: new CookieCollection()))
            {
                Assert.AreEqual(200, (int)response.StatusCode);
                Assert.AreEqual("OK", response.StatusDescription);
                Assert.AreEqual(-1, response.ContentLength);
                Assert.AreEqual("text/plain; charset=utf-8", response.ContentType);
                Assert.AreEqual("gzip", response.ContentEncoding);
                Assert.AreEqual("utf-8", response.CharacterSet);

                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    var content = reader.ReadToEnd();
                    Assert.AreEqual("record1 från cassette3", content);
                }

                Assert.AreEqual(10, response.Headers.Count);
                Assert.AreEqual("200 OK", response.Headers["Status"]);
                Assert.AreEqual("http://test.com/redirect-url", response.Headers["Location"]);
                Assert.AreEqual("request1_record1_cassette3", response.Headers["X-Request-Id"]);
                Assert.AreEqual("public, max-age=60, s-maxage=60", response.Headers["Cache-Control"]);
                Assert.AreEqual("text/plain; charset=utf-8", response.Headers["Content-Type"]);
                Assert.AreEqual("gzip", response.Headers["Content-Encoding"]);
                Assert.AreEqual("chunked", response.Headers["Transfer-Encoding"]);
                Assert.AreEqual("cookie1=value1, cookie2=value2", response.Headers["Set-Cookie"]);

                Assert.AreEqual(2, response.Cookies.Count);
                Assert.AreEqual(new Cookie("cookie1", "value1", "/request1", "localhost"), response.Cookies[0]);
                Assert.AreEqual(new Cookie("cookie2", "value2", "/request1", "localhost"), response.Cookies[1]);
            }
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
                Assert.AreEqual(454, (int)response.StatusCode);
                Assert.AreEqual("Player request mismatch", response.StatusDescription);
            }

            player.Stop();
            player.Play("record1");

            // wrong method
            ex = Assert.Throws<WebException>(() => client.Send("/request1", "POST", "content"));
            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual(454, (int)response.StatusCode);
            }

            // wrong content
            ex = Assert.Throws<WebException>(() => client.Send("/request2", "POST", "dålig"));
            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual(454, (int)response.StatusCode);
            }

            // mismatching headers list
            ex = Assert.Throws<WebException>(() => client.Send("/request3", "GET", headers: new NameValueCollection {{ "Custom-Header", "wrong" }}));
            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual(454, (int)response.StatusCode);
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
                Assert.AreEqual(454, (int)response.StatusCode);
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
                Assert.AreEqual(551, (int)response.StatusCode);
                Assert.AreEqual("Player exception", response.StatusDescription);
            }
        }

        [Test]
        public void Recording_RedirectsRequestToRemoteAddress()
        {
            player.Start();
            player.Load(new Cassette(Context.CreateCassette("new")));
            player.Record("record1");

            Action<HttpListenerRequest, HttpListenerResponse> callback = delegate (HttpListenerRequest request, HttpListenerResponse response)
            {
                Assert.AreEqual("/path", request.RawUrl);
                Assert.AreEqual("POST", request.HttpMethod);

                Assert.IsTrue(request.HasEntityBody);

                using (var stream = request.InputStream)
                using (var reader = new StreamReader(stream, request.ContentEncoding))
                {
                     Assert.AreEqual("request", reader.ReadToEnd());
                }

                if (request.Headers["Connection"] == null)
                {
                    Assert.AreEqual(11, request.Headers.Count);
                }
                else
                {
                    Assert.AreEqual(12, request.Headers.Count);
                    Assert.AreEqual("Keep-Alive", request.Headers["Connection"]);
                }

                Assert.AreEqual("text/plain", request.Headers["Accept"]);
                Assert.AreEqual(new [] { "text/plain" }, request.AcceptTypes);

                Assert.AreEqual(true, request.KeepAlive);

                Assert.AreEqual(-1, request.ContentLength64);

                Assert.AreEqual("text/plain; charset=ascii", request.Headers["Content-Type"]);
                Assert.AreEqual("text/plain; charset=ascii", request.ContentType);
                Assert.AreEqual(Encoding.ASCII, request.ContentEncoding);

                Assert.AreEqual("Wed, 01 Jun 2016 08:12:31 GMT", request.Headers["Date"]);
                Assert.AreEqual("random,100-continue", request.Headers["Expect"]);
                Assert.AreEqual("Wed, 01 Jun 2016 07:00:00 GMT", request.Headers["If-Modified-Since"]);

                Assert.AreEqual("http://test.com", request.Headers["Referer"]);
                Assert.AreEqual(new Uri("http://test.com"), request.UrlReferrer);

                Assert.AreEqual("chunked", request.Headers["Transfer-Encoding"]);

                Assert.AreEqual("test client", request.Headers["User-Agent"]);
                Assert.AreEqual("test client", request.UserAgent);

                Assert.AreEqual("value", request.Headers["Custom-Header"]);

                Assert.AreEqual("cookie1=value1; cookie2=value2", request.Headers["Cookie"]);
                Assert.AreEqual(2, request.Cookies.Count);
                Assert.AreEqual(new Cookie("cookie1", "value1"), request.Cookies[0]);
                Assert.AreEqual(new Cookie("cookie2", "value2"), request.Cookies[1]);
            };

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() =>
                {
                    var client = new Client(baseAddress);

                    var headers = new NameValueCollection
                    {
                        { "Accept", "text/plain" },
                        { "Connection", "Keep-Alive" },
                        { "Content-Type", "text/plain; charset=ascii" },
                        { "Date", "Wed, 01 Jun 2016 08:12:31 GMT" },
                        { "Expect", "random" },
                        { "If-Modified-Since", "Wed, 01 Jun 2016 07:00:00 GMT" },
                        { "Referer", "http://test.com" },
                        { "Transfer-Encoding", "chunked" },
                        { "User-Agent", "test client" },
                        { "Custom-Header", "value" }
                    };

                    var cookies = new CookieCollection();
                    cookies.Add(new Cookie()
                    {
                        Name = "cookie1",
                        Value = "value1",
                        Domain = baseAddress.Host
                    });
                    cookies.Add(new Cookie()
                    {
                        Name = "cookie2",
                        Value = "value2",
                        Domain = baseAddress.Host
                    });

                    client.Send("/path", "POST", "request", headers, cookies);
                });

                server.Accept(callback);
            }
        }

        [Test]
        public void Recording_CanHandleMultipleRemoteAddresses()
        {
            player.Start();
            player.Load(new Cassette(Context.CreateCassette("new")));
            player.Record("record1");

            bool requestProcessed1 = false;
            bool requestProcessed2 = false;

            Action<HttpListenerRequest, HttpListenerResponse> callback1 = delegate (HttpListenerRequest request, HttpListenerResponse response)
            {
                if(requestProcessed2)
                {
                    Assert.Fail();
                }

                requestProcessed1 = true;
            };

            Action<HttpListenerRequest, HttpListenerResponse> callback2 = delegate (HttpListenerRequest request, HttpListenerResponse response)
            {
                if (!requestProcessed1)
                {
                    Assert.Fail();
                }

                requestProcessed2 = true;
            };

            var client = new Client(baseAddress);

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() =>
                {
                    client.Send("/path", "GET");
                });

                server.Accept(callback1);
            }

            player.RemoteAddress = remoteAddress2;

            using (var server = new Server(remoteAddress2))
            {
                Task.Run(() =>
                {
                    client.Send("/path", "GET");
                });

                server.Accept(callback2);
            }
        }

        [Test]
        public void Recording_ResponsesWithRemoteResponse()
        {
            player.Start();
            player.Load(new Cassette(Context.CreateCassette("new")));
            player.Record("record1");

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() => server.Accept());

                var client = new Client(baseAddress);

                using (var response = (HttpWebResponse)client.Send("/path", "GET", cookies: new CookieCollection()))
                {
                    Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
                    Assert.AreEqual("Hurrah!", response.StatusDescription);

                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.ASCII))
                    {
                        Assert.AreEqual("response", reader.ReadToEnd());
                    }

                    Assert.AreEqual(9, response.Headers.Count);

                    Assert.AreEqual("8", response.Headers["Content-Length"]);
                    Assert.AreEqual(8, response.ContentLength);

                    Assert.AreEqual("text/plain; charset=ascii", response.Headers["Content-Type"]);
                    Assert.AreEqual("text/plain; charset=ascii", response.ContentType);
                    Assert.AreEqual("ascii", response.CharacterSet);

                    Assert.AreEqual("us-ascii", response.Headers["Content-Encoding"]);
                    Assert.AreEqual("us-ascii", response.ContentEncoding);

                    Assert.AreEqual("http://test.com", response.Headers["Location"]);
                    Assert.AreEqual("close", response.Headers["Connection"]);
                    Assert.AreEqual("value", response.Headers["Custom-Header"]);

                    Assert.AreEqual("cookie1=value1, cookie2=value2", response.Headers["Set-Cookie"]);
                    Assert.AreEqual(2, response.Cookies.Count);
                    Assert.AreEqual(new Cookie("cookie1", "value1", "/path", "localhost"), response.Cookies[0]);
                    Assert.AreEqual(new Cookie("cookie2", "value2", "/path", "localhost"), response.Cookies[1]);
                }
            }
        }

        [Test]
        public void Recording_WebException_ResponsesWithWebExceptionResponse()
        {
            player.Start();
            player.Load(new Cassette(Context.CreateCassette("new")));
            player.Record("record1");

            Action<HttpListenerRequest, HttpListenerResponse> callback = delegate (HttpListenerRequest request, HttpListenerResponse response)
            {
                Assert.AreEqual(400, response.StatusCode);
                Assert.AreEqual("Wrong", response.StatusDescription);
            };

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() =>
                {
                    var client = new Client(baseAddress);
                    client.Send("/path", "GET");
                });

                server.Reject(callback);
            }
        }

        [Test]
        public void Recording_Exception_ResponsesWithPlayerError()
        {
            player.Start();
            player.Load(new Cassette(Context.CreateCassette("new")));
            player.Record("record1");

            var client = new Client(baseAddress);

            var ex = Assert.Throws<WebException>(() => client.Send("/", "GET"));

            using (var response = (HttpWebResponse)ex.Response)
            {
                Assert.AreEqual(552, (int)response.StatusCode);
                Assert.AreEqual("Player exception", response.StatusDescription);
            }
        }

        [Test]
        public void Recording_PostEmptyContent_ContentLengthHeaderSaved()
        {
            var cassette = new Cassette(Context.CreateCassette("new"));

            player.Start();
            player.Load(cassette);
            player.Record("record1");

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() => server.Accept());

                var client = new Client(baseAddress);
                client.Send("/", "POST", headers: new NameValueCollection {{ "Content-Length", "0" }});
            }
            player.Stop();

            var record = cassette.Find("record1");
            var mock = (JObject)record.Read();
            var jrequest = (JObject)mock["request"];

            Assert.IsNull(jrequest["content"]);
            Assert.AreEqual("0", jrequest["headers"]["Content-Length"].ToString());
        }

        [Test]
        public void Recording_HandlesBinaryContent()
        {
            long requestContentLength = 0, responseContentLength = 0;

            var cassette = new Cassette(Context.CreateCassette("new"));

            player.Start();
            player.Load(cassette);
            player.Record("record1");

            Action<HttpListenerRequest, HttpListenerResponse> callback = delegate (HttpListenerRequest request, HttpListenerResponse response)
            {
                response.StatusCode = 200;
                response.StatusDescription = "Get your file";

                response.KeepAlive = false;
                response.ContentType = "image/gif; charset=ascii";

                using (var bitmap = new Bitmap(40, 50))
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Gif);

                    var content = stream.ToArray();
                    responseContentLength = response.ContentLength64 = content.Length;

                    response.OutputStream.Write(content, 0, content.Length);
                }
            };

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() => server.Accept(callback));

                var client = new Client(baseAddress);

                var headers = new NameValueCollection
                {
                    { "Connection", "Keep-Alive" },
                    { "Content-Type", "image/png" },
                    { "Date", "Sat, 13 Oct 2018 18:12:00 GMT" },
                    { "User-Agent", "test client" },
                    { "Cache-Control", "no - cache" }
                };

                using (var bitmap = new Bitmap(10, 20))
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);

                    requestContentLength = stream.Length;

                    client.Send("/path", "POST", stream.ToArray(), headers);
                }
            }

            player.Stop();

            var record = cassette.Find("record1");
            var mock = (JObject)record.Read();
            var jrequest = (JObject)mock["request"];
            var jresponse = (JObject)mock["response"];

            Assert.AreEqual(requestContentLength, (int)jrequest["headers"]["Content-Length"]);
            Assert.AreEqual(200, (int)jresponse["statusCode"]);
            Assert.AreEqual(responseContentLength, (int)jresponse["headers"]["Content-Length"]);
        }

        [Test]
        public void Recording_EmptyRecord_NotSaved()
        {
            var path = Context.CreateCassette("new");

            player.Start();
            player.Load(new Cassette(path));
            player.Record("record1");

            var client = new Client(baseAddress);

            try
            {
                client.Send("/", "GET");
            }
            catch { }

            player.Stop();

            Assert.IsFalse(File.Exists(path));
        }

        [Test]
        public void Stop_AfterPlayed_RewindsRecord()
        {
            player.Start();
            player.Load(cassette1);
            player.Play("record2");

            var client = new Client(baseAddress);
            client.Send("/user", "GET");

            player.Stop();
            player.Play("record2");

            Assert.DoesNotThrow(() => client.Send("/user", "GET"));
        }

        [Test]
        public void Stop_AfterRecorded_RewindsRecord()
        {
            player.Start();
            player.Load(new Cassette(Context.CreateCassette("new")));
            player.Record("record1");

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() => server.Accept());

                var client = new Client(baseAddress);
                client.Send("/path", "GET");

                player.Stop();
                player.Play("record1");

                Assert.DoesNotThrow(() => client.Send("/path", "GET"));
            }
        }

        [Test]
        public void Stop_AfterRecorded_SavesRecord()
        {
            var cassette = new Cassette(Context.CreateCassette("new"));

            player.Start();
            player.Load(cassette);
            player.Record("record1");

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() => server.Accept());

                var client = new Client(baseAddress);

                var headers = new NameValueCollection
                {
                    { "Accept", "text/plain" },
                    { "Connection", "Keep-Alive" },
                    { "Content-Type", "text/plain; charset=ascii" },
                    { "Date", "Wed, 01 Jun 2016 08:12:31 GMT" },
                    { "Expect", "random" },
                    { "If-Modified-Since", "Wed, 01 Jun 2016 07:00:00 GMT" },
                    { "Referer", "http://test.com" },
                    { "Transfer-Encoding", "chunked" },
                    { "User-Agent", "test client" },
                    { "Custom-Header", "value" }
                };

                var cookies = new CookieCollection();
                cookies.Add(new Cookie()
                {
                    Name = "cookie1",
                    Value = "value1",
                    Domain = baseAddress.Host
                });
                cookies.Add(new Cookie()
                {
                    Name = "cookie2",
                    Value = "value2",
                    Domain = baseAddress.Host
                });

                client.Send("/path", "POST", "request", headers, cookies);
            }

            player.Stop();

            var record = cassette.Find("record1");
            var mock = (JObject)record.Read();
            var jrequest = (JObject)mock["request"];
            var jresponse = (JObject)mock["response"];

            Assert.AreEqual(1, record.List.Count);

            Assert.AreEqual("POST", jrequest["method"].ToString());
            Assert.AreEqual("http://localhost:5560/path", jrequest["uri"].ToString());
            Assert.AreEqual("request", jrequest["content"].ToString());

            var requestHeadersCount = ((JObject)jrequest["headers"]).Count;
            if(jrequest["headers"]["Connection"] == null)
            {
                Assert.AreEqual(11, requestHeadersCount);
            }
            else
            {
                Assert.AreEqual(12, requestHeadersCount);
                Assert.AreEqual("Keep-Alive", jrequest["headers"]["Connection"].ToString());
            }
            Assert.AreEqual("value", jrequest["headers"]["Custom-Header"].ToString());
            Assert.AreEqual("Wed, 01 Jun 2016 08:12:31 GMT", jrequest["headers"]["Date"].ToString());
            Assert.AreEqual("chunked", jrequest["headers"]["Transfer-Encoding"].ToString());
            Assert.AreEqual("text/plain; charset=ascii", jrequest["headers"]["Content-Type"].ToString());
            Assert.AreEqual("text/plain", jrequest["headers"]["Accept"].ToString());
            Assert.AreEqual("cookie1=value1; cookie2=value2", jrequest["headers"]["Cookie"].ToString());
            Assert.AreEqual("random,100-continue", jrequest["headers"]["Expect"].ToString());
            Assert.AreEqual("localhost:5560", jrequest["headers"]["Host"].ToString());
            Assert.AreEqual("Wed, 01 Jun 2016 07:00:00 GMT", jrequest["headers"]["If-Modified-Since"].ToString());
            Assert.AreEqual("http://test.com", jrequest["headers"]["Referer"].ToString());
            Assert.AreEqual("test client", jrequest["headers"]["User-Agent"].ToString());

            Assert.AreEqual(200, (int)jresponse["statusCode"]);
            Assert.AreEqual("Hurrah!", jresponse["statusDescription"].ToString());
            Assert.AreEqual("response", jresponse["content"].ToString());

            Assert.AreEqual(9, ((JObject)jresponse["headers"]).Count);
            Assert.AreEqual("us-ascii", jresponse["headers"]["Content-Encoding"].ToString());
            Assert.AreEqual("value", jresponse["headers"]["Custom-Header"].ToString());
            Assert.AreEqual("close", jresponse["headers"]["Connection"].ToString());
            Assert.AreEqual("8", jresponse["headers"]["Content-Length"].ToString());
            Assert.AreEqual("text/plain; charset=ascii", jresponse["headers"]["Content-Type"].ToString());
            Assert.IsNotNull(jresponse["headers"]["Date"]);
            Assert.AreEqual("http://test.com", jresponse["headers"]["Location"].ToString());
            Assert.AreEqual("cookie1=value1, cookie2=value2", jresponse["headers"]["Set-Cookie"].ToString());
            Assert.IsNotNull(jresponse["headers"]["Server"]);
        }

        [Test]
        public void Close_NotAcceptsRequests()
        {
            player.Start();
            player.Close();

            using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                var ex = Assert.Throws<SocketException>(() => socket.Connect(baseAddress.Host, baseAddress.Port));

                Assert.AreEqual(SocketError.ConnectionRefused, ex.SocketErrorCode);
            }
        }

        [Test]
        public void Close_SetsState()
        {
            player.Start();
            player.Close();

            Assert.AreEqual(Player.State.Off, player.CurrentState);
        }

        [Test]
        public void Close_AfterPlayed_RewindsRecord()
        {
            player.Start();
            player.Load(cassette1);
            player.Play("record2");

            var client = new Client(baseAddress);
            client.Send("/user", "GET");

            player.Close();

            player = new Player(baseAddress, remoteAddress1);
            player.Start();
            player.Load(cassette1);
            player.Play("record2");

            Assert.DoesNotThrow(() => client.Send("/user", "GET"));
        }

        [Test]
        public void Close_AfterRecorded_RewindsRecord()
        {
            var cassette = new Cassette(Context.CreateCassette("new"));

            player.Start();
            player.Load(cassette);
            player.Record("record1");

            using (var server = new Server(remoteAddress1))
            {
                Task.Run(() => server.Accept());

                var client = new Client(baseAddress);
                client.Send("/path", "GET");

                player.Close();

                player = new Player(baseAddress, remoteAddress1);
                player.Start();
                player.Load(cassette);
                player.Play("record1");

                Assert.DoesNotThrow(() => client.Send("/path", "GET"));
            }
        }
    }
}
