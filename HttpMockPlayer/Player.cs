using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Dynamic;
using System.Net.Mime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Signed assemblies cannot build on Travis, because the key file is not committed.
// [assembly: InternalsVisibleTo("HttpMockPlayer.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010027ff4de06098a639e8c798c860211683b8899f1db89df350b838ea986efc79986e743667171fd42826e9cac176c831ce4809bb3800d5dcfdb6c5467e30fd2db34d8e6e5970ccfdc2f99a6ae0247ae430a1ecafc1e984d53323b22191a2b23af7318faa9525c57e1025ffbf30beaaac6f4fc269f621aa88bc9127ea446b0e8394")]

[assembly: InternalsVisibleTo("HttpMockPlayer.Tests")]

namespace HttpMockPlayer
{
    /// <summary>
    /// Serves as player and/or recorder of HTTP requests.
    /// </summary>
    public class Player
    {
        private Uri baseAddress, remoteAddress;
        private HttpListener httpListener;
        private Cassette cassette;
        private Record record;

        // mutex object, used to avoid collisions when processing 
        // an incoming request or updating the player state value
        private object statelock;

        /// <summary>
        /// Initializes a new instance of the <see cref="Player"/> class with base and remote address URI's.
        /// </summary>
        /// <param name="baseAddress">URI address on which play or record requests are accepted.</param>
        /// <param name="remoteAddress">URI address of the Internet resource being mocked.</param>
        /// <exception cref="ArgumentNullException"/>
        public Player(Uri baseAddress, Uri remoteAddress)
        {
            if (baseAddress == null)
            {
                throw new ArgumentNullException("baseAddress");
            }
            if (remoteAddress == null)
            {
                throw new ArgumentNullException("remoteAddress");
            }

            this.baseAddress = baseAddress;
            this.remoteAddress = remoteAddress;

            var baseAddressString = baseAddress.OriginalString;
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(baseAddressString.EndsWith("/") ? baseAddressString : baseAddressString + "/");

            statelock = new object();
        }

        /// <summary>
        /// Gets URI address on which play or record requests are accepted.
        /// </summary>
        public Uri BaseAddress
        {
            get
            {
                return baseAddress;
            }
        }

        /// <summary>
        /// Gets or sets URI address of the Internet resource being mocked.
        /// </summary>
        /// <exception cref="ArgumentNullException"/>
        public Uri RemoteAddress
        {
            get
            {
                return remoteAddress;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                remoteAddress = value;
            }
        }

        /// <summary>
        /// Gets current state of this <see cref="Player"/> object.
        /// </summary>
        public State CurrentState { get; private set; }

        #region Mock request/response

        private enum PlayerErrorCode
        {
            RequestNotFound = 454,
            Exception = 550,
            PlayException = 551,
            RecordException = 552
        }

        private class MockRequest
        {
            private MockRequest() { }

            internal string Method { get; private set; }

            internal string Uri { get; private set; }

            internal string Content { get; private set; }

            internal NameValueCollection Headers { get; private set; }

            internal JObject ToJson()
            {
                var jrequest = new JObject();

                jrequest.Add("method", Method);
                jrequest.Add("uri", Uri);

                if (Content != null)
                {
                    try
                    {
                        var jcontent = JToken.Parse(Content);
                        jrequest.Add("content", jcontent);
                    }
                    catch (JsonReaderException)
                    {
                        jrequest.Add("content", Content);
                    }
                }

                if (Headers != null)
                {
                    var jheaders = new JObject();
                    foreach (var header in Headers.AllKeys)
                    {
                        jheaders.Add(header, Headers[header]);
                    }
                    jrequest.Add("headers", jheaders);
                }

                return jrequest;
            }

            internal static MockRequest FromJson(JObject jrequest)
            {
                var mockRequest = new MockRequest()
                {
                    Method = jrequest["method"].ToString(),
                    Uri = jrequest["uri"].ToString()
                };

                if (jrequest["content"] != null)
                {
                    var content = jrequest["content"].ToString();

                    try
                    {
                        var jcontent = JToken.Parse(content);
                        mockRequest.Content = JsonConvert.SerializeObject(jcontent);
                    }
                    catch (JsonReaderException)
                    {
                        mockRequest.Content = content;
                    }
                }

                if (jrequest["headers"] != null)
                {
                    mockRequest.Headers = new NameValueCollection();
                    foreach (JProperty jheader in jrequest["headers"])
                    {
                        mockRequest.Headers.Add(jheader.Name, jheader.Value.ToString());
                    }
                }

                return mockRequest;
            }

            internal static MockRequest FromHttpRequest(Uri uri, HttpListenerRequest request)
            {
                var mockRequest = new MockRequest()
                {
                    Method = request.HttpMethod,
                    Uri = uri.OriginalString + request.Url.PathAndQuery
                };

                if (request.HasEntityBody)
                {
                    var contentType = new ContentType(request.Headers?.Get("Content-Type") ?? "text/plain; charset=utf-8");
                    var encoding = GetEncoding(contentType.CharSet);

                    using (var stream = request.InputStream)
                    using (var reader = new StreamReader(stream, encoding))
                    {
                        var content = reader.ReadToEnd();

                        try
                        {
                            var jcontent = JToken.Parse(content);
                            mockRequest.Content = JsonConvert.SerializeObject(jcontent);
                        }
                        catch (JsonReaderException)
                        {
                            mockRequest.Content = content;
                        }
                    }
                }

                if(request.Headers != null && request.Headers.Count > 0)
                {
                    mockRequest.Headers = new NameValueCollection(request.Headers);

                    if (request.Headers["Host"] != null)
                    {
                        mockRequest.Headers["Host"] = uri.Authority;
                    }
                }

                return mockRequest;
            }

            private bool IsEqual(MockRequest mockRequest)
            {
                if (!string.Equals(Method, mockRequest.Method))
                {
                    return false;
                }

                if(!string.Equals(Uri, mockRequest.Uri))
                {
                    return false;
                }

                if(!string.Equals(Content, mockRequest.Content))
                {
                    return false;
                }

                NameValueCollection headers = null;

                // presence of Connection=Keep-Alive header is not persistent and depends on request order,
                // so it is skipped or added to match the corresponding header in the fellow request
                if(Headers != null)
                {
                    headers = new NameValueCollection(Headers);

                    if (headers["Connection"] == "Keep-Alive" &&
                        mockRequest.Headers?.Get("Connection") == null)
                    {
                        headers.Remove("Connection");
                    }

                    if (headers["Connection"] == null &&
                        mockRequest.Headers?.Get("Connection") == "Keep-Alive")
                    {
                        headers.Add("Connection", "Keep-Alive");
                    }
                }

                if((headers == null) != (mockRequest.Headers == null))
                {
                    return false;
                }
                if (headers != null)
                {
                    if (headers.Count != mockRequest.Headers.Count)
                    {
                        return false;
                    }
                    foreach (string header in headers)
                    {
                        if (!string.Equals(headers[header], mockRequest.Headers[header]))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj))
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                if (GetType() != obj.GetType())
                {
                    return false;
                }

                return IsEqual((MockRequest)obj);
            }

            public bool Equals(MockRequest mockRequest)
            {
                if (ReferenceEquals(null, mockRequest))
                {
                    return false;
                }

                if (ReferenceEquals(this, mockRequest))
                {
                    return true;
                }

                return IsEqual(mockRequest);
            }

            public override int GetHashCode()
            {
                return Tuple.Create(Method, Uri, Content).GetHashCode();
            }
        }

        private class MockResponse
        {
            private MockResponse() { }

            internal int StatusCode { get; private set; }

            internal string StatusDescription { get; private set; }

            internal string Content { get; private set; }

            internal WebHeaderCollection Headers { get; private set; }

            internal JObject ToJson()
            {
                var jresponse = new JObject();

                jresponse.Add("statusCode", StatusCode);
                jresponse.Add("statusDescription", StatusDescription);

                if (Content != null)
                {
                    try
                    {
                        var jcontent = JToken.Parse(Content);
                        jresponse.Add("content", jcontent);
                    }
                    catch (JsonReaderException)
                    {
                        jresponse.Add("content", Content);
                    }
                }

                if (Headers != null)
                {
                    var jheaders = new JObject();
                    foreach (var header in Headers.AllKeys)
                    {
                        jheaders.Add(header, Headers[header]);
                    }
                    jresponse.Add("headers", jheaders);
                }

                return jresponse;
            }

            internal static MockResponse FromJson(JObject jresponse)
            {
                var mockResponse = new MockResponse()
                {
                    StatusCode = jresponse["statusCode"].ToObject<int>(),
                    StatusDescription = jresponse["statusDescription"].ToString()
                };

                if (jresponse["content"] != null)
                {
                    var content = jresponse["content"].ToString();

                    try
                    {
                        var jcontent = JToken.Parse(content);
                        mockResponse.Content = JsonConvert.SerializeObject(jcontent);
                    }
                    catch (JsonReaderException)
                    {
                        mockResponse.Content = content;
                    }
                }

                if (jresponse["headers"] != null)
                {
                    mockResponse.Headers = new WebHeaderCollection();
                    foreach (JProperty jheader in jresponse["headers"])
                    {
                        mockResponse.Headers.Add(jheader.Name, jheader.Value.ToString());
                    }
                }

                return mockResponse;
            }

            internal static MockResponse FromHttpResponse(HttpWebResponse response)
            {
                var mockResponse = new MockResponse()
                {
                    StatusCode = (int)response.StatusCode,
                    StatusDescription = response.StatusDescription
                };

                if (response.ContentLength != 0)
                {
                    var encoding = GetEncoding(response.CharacterSet);

                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream, encoding))
                    {
                        var content = reader.ReadToEnd();

                        try
                        {
                            var jcontent = JToken.Parse(content);
                            mockResponse.Content = JsonConvert.SerializeObject(jcontent);
                        }
                        catch (JsonReaderException)
                        {
                            mockResponse.Content = content;
                        }
                    }
                }

                if(response.Headers != null && response.Headers.Count > 0)
                {
                    mockResponse.Headers = response.Headers;
                }

                return mockResponse;
            }

            internal static MockResponse FromPlayerError(PlayerErrorCode errorCode, string status, string message)
            {
                return new MockResponse()
                {
                    StatusCode = (int)errorCode,
                    StatusDescription = status,
                    Content = message
                };
            }
        }

        #endregion

        #region Http request/response

        private HttpWebRequest BuildRequest(MockRequest mockRequest)
        {
            var request = WebRequest.CreateHttp(new Uri(mockRequest.Uri));

            request.Method = mockRequest.Method;

            if (mockRequest.Headers != null)
            {
                foreach (string header in mockRequest.Headers)
                {
                    var value = mockRequest.Headers[header];

                    switch (header)
                    {
                        case "Accept":
                            request.Accept = value;
                            break;
                        case "Connection":
                            if (value.ToLower() == "keep-alive")
                            {
                                request.KeepAlive = true;
                            }
                            else if (value.ToLower() == "close")
                            {
                                request.KeepAlive = false;
                            }
                            else
                            {
                                request.Connection = value;
                            }
                            break;
                        case "Content-Length":
                            request.ContentLength = long.Parse(value);
                            break;
                        case "Content-Type":
                            request.ContentType = value;
                            break;
                        case "Date":
                            request.Date = DateTime.Parse(value);
                            break;
                        case "Expect":
                            var list = value.Split(',');
                            var values = list.Where(v => v != "100-continue");

                            value = string.Join(",", values);

                            if (!string.IsNullOrEmpty(value))
                            {
                                request.Expect = value;
                            }
                            break;
                        case "Host":
                            request.Host = value;
                            break;
                        case "If-Modified-Since":
                            request.IfModifiedSince = DateTime.Parse(value);
                            break;
                        case "Referer":
                            request.Referer = value;
                            break;
                        case "Transfer-Encoding":
                            if (value.ToLower() == "chunked")
                            {
                                request.SendChunked = true;
                            }
                            else
                            {
                                request.TransferEncoding = value;
                            }
                            break;
                        case "User-Agent":
                            request.UserAgent = value;
                            break;
                        default:
                            request.Headers[header] = value;
                            break;
                    }
                }
            }

            if (mockRequest.Content != null)
            {
                var contentType = new ContentType(mockRequest.Headers?.Get("Content-Type") ?? "text/plain; charset=utf-8");
                var encoding = GetEncoding(contentType.CharSet);
                var content = encoding.GetBytes(mockRequest.Content);

                if (mockRequest.Headers?.Get("Content-Length") != null)
                {
                    request.ContentLength = content.Length;
                }

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(content, 0, content.Length);
                }
            }

            return request;
        }

        private static Encoding GetEncoding(string charSet)
        {
            return Encoding.GetEncoding(string.IsNullOrEmpty(charSet) 
                ? "utf-8" 
                : charSet);
        }

        private void BuildResponse(HttpListenerResponse response, MockResponse mockResponse)
        {
            response.StatusCode = mockResponse.StatusCode;
            response.StatusDescription = mockResponse.StatusDescription;

            response.Headers.Clear();

            if (mockResponse.Headers != null)
            {
                foreach (string header in mockResponse.Headers)
                {
                    var value = mockResponse.Headers[header];

                    switch (header)
                    {
                        case "Connection":
                            response.KeepAlive = (value.ToLower() == "keep-alive");
                            break;
                        case "Content-Length":
                            break;
                        case "Content-Type":
                            response.ContentType = value;
                            break;
                        case "Location":
                            response.RedirectLocation = value;
                            break;
                        case "Transfer-Encoding":
                            response.SendChunked = (value.ToLower() == "chunked");
                            break;
                        default:
                            response.Headers[header] = value;
                            break;
                    }
                }
            }

            if (mockResponse.Content != null)
            {
                var contentType = new ContentType(mockResponse.Headers?.Get("Content-Type") ?? "text/plain; charset=utf-8");
                var encoding = GetEncoding(contentType.CharSet);
                var content = encoding.GetBytes(mockResponse.Content);

                if(mockResponse.Headers?.Get("Content-Length") != null)
                {
                    response.ContentLength64 = content.Length;
                }

                using (var stream = response.OutputStream)
                {
                    stream.Write(content, 0, content.Length);
                }
            }
        }

        #endregion

        #region State

        /// <summary>
        /// Represents state of a <see cref="Player"/> object.
        /// </summary>
        public enum State
        {
            Off,
            Idle,
            Playing,
            Recording
        }

        /// <summary>
        /// Allows this instance to receive and process incoming requests.
        /// </summary>
        /// <exception cref="PlayerStateException"/>
        public void Start()
        {
            lock (statelock)
            {
                if (CurrentState != State.Off)
                {
                    throw new PlayerStateException(CurrentState, "Player has already started.");
                }

                httpListener.Start();

                Task.Run(() =>
                {
                    while (httpListener.IsListening)
                    {
                        var context = httpListener.GetContext();
                        var playerRequest = context.Request;
                        var playerResponse = context.Response;

                        lock (statelock)
                        {
                            try
                            {
                                switch (CurrentState)
                                {
                                    case State.Playing:
                                        {
                                            var mock = (JObject)record.Read();
                                            var mockRequest = MockRequest.FromJson((JObject)mock["request"]);
                                            var mockPlayerRequest = MockRequest.FromHttpRequest(remoteAddress, playerRequest);

                                            MockResponse mockResponse;

                                            if (mockRequest.Equals(mockPlayerRequest))
                                            {
                                                mockResponse = MockResponse.FromJson((JObject)mock["response"]);
                                            }
                                            else
                                            {
                                                mockResponse = MockResponse.FromPlayerError(PlayerErrorCode.RequestNotFound, "Player request mismatch", $"Player could not play the request at {playerRequest.Url.PathAndQuery}. The request doesn't match the current recorded one.");
                                            }

                                            BuildResponse(playerResponse, mockResponse);
                                        }

                                        break;
                                    case State.Recording:
                                        {
                                            var mockRequest = MockRequest.FromHttpRequest(remoteAddress, playerRequest);
                                            var request = BuildRequest(mockRequest);

                                            MockResponse mockResponse;

                                            try
                                            {
                                                using (var response = (HttpWebResponse)request.GetResponse())
                                                {
                                                    mockResponse = MockResponse.FromHttpResponse(response);
                                                }
                                            }
                                            catch (WebException ex)
                                            {
                                                if(ex.Response == null)
                                                {
                                                    throw ex;
                                                }

                                                mockResponse = MockResponse.FromHttpResponse((HttpWebResponse)ex.Response);
                                            }

                                            var mock = JObject.FromObject(new
                                            {
                                                request = mockRequest.ToJson(),
                                                response = mockResponse.ToJson()
                                            });
                                            record.Write(mock);

                                            BuildResponse(playerResponse, mockResponse);
                                        }

                                        break;
                                    default:
                                        throw new PlayerStateException(CurrentState, "Player is not in operation.");
                                }
                            }
                            catch (Exception ex)
                            {
                                PlayerErrorCode errorCode;
                                string process;

                                switch (CurrentState)
                                {
                                    case State.Playing:
                                        errorCode = PlayerErrorCode.PlayException;
                                        process = "play";

                                        break;
                                    case State.Recording:
                                        errorCode = PlayerErrorCode.RecordException;
                                        process = "record";

                                        break;
                                    default:
                                        errorCode = PlayerErrorCode.Exception;
                                        process = "process";

                                        break;
                                }

                                var mockResponse = MockResponse.FromPlayerError(errorCode, "Player exception", $"Player could not {process} the request at {playerRequest.Url.PathAndQuery} because of exception: {ex}");

                                BuildResponse(playerResponse, mockResponse);
                            }
                            finally
                            {
                                playerResponse.Close();
                            }
                        }
                    }
                });

                CurrentState = State.Idle;
            }
        }

        /// <summary>
        /// Sets this instance to <see cref="State.Playing"/> state and loads a mock record for replaying.
        /// </summary>
        /// <param name="name">Name of the record to replay.</param>
        /// <exception cref="PlayerStateException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="ArgumentException"/>
        public void Play(string name)
        {
            lock (statelock)
            {
                if (CurrentState == State.Off)
                {
                    throw new PlayerStateException(CurrentState, "Player is not started.");
                }

                if (CurrentState != State.Idle)
                {
                    throw new PlayerStateException(CurrentState, "Player is already in operation.");
                }

                if (cassette == null)
                {
                    throw new InvalidOperationException("Cassette is not loaded.");
                }

                record = cassette.Find(name);
                if (record == null)
                {
                    throw new ArgumentException($"Cassette doesn't contain a record with the given name: {name}.");
                }

                CurrentState = State.Playing;
            }
        }

        /// <summary>
        /// Sets this instance to <see cref="State.Recording"/> state and creates a new mock record for recording. 
        /// </summary>
        /// <param name="name">Name of the new record.</param>
        /// <exception cref="PlayerStateException"/>
        /// <exception cref="InvalidOperationException"/>
        public void Record(string name)
        {
            lock (statelock)
            {
                if (CurrentState == State.Off)
                {
                    throw new PlayerStateException(CurrentState, "Player is not started.");
                }

                if (CurrentState != State.Idle)
                {
                    throw new PlayerStateException(CurrentState, "Player is already in operation.");
                }

                if (cassette == null)
                {
                    throw new InvalidOperationException("Cassette is not loaded.");
                }

                record = new Record(name);

                CurrentState = State.Recording;
            }
        }

        /// <summary>
        /// Sets this instance to <see cref="State.Idle"/> state.
        /// </summary>
        /// <exception cref="PlayerStateException"/>
        public void Stop()
        {
            lock (statelock)
            {
                if (CurrentState == State.Off)
                {
                    throw new PlayerStateException(CurrentState, "Player is not started.");
                }

                if (CurrentState != State.Idle)
                {
                    record.Rewind();

                    if (CurrentState == State.Recording && !record.IsEmpty())
                    {
                        cassette.Save(record);
                    }

                    record = null;

                    CurrentState = State.Idle;
                }
            }
        }

        #endregion

        /// <summary>
        /// Loads a cassette to this <see cref="Player"/> object.
        /// </summary>
        /// <param name="cassette">Cassette to load.</param>
        public void Load(Cassette cassette)
        {
            this.cassette = cassette;
        }

        /// <summary>
        /// Shuts down this <see cref="Player"/> object.
        /// </summary>
        public void Close()
        {
            lock (statelock)
            {
                if (CurrentState != State.Off)
                {
                    if (CurrentState != State.Idle)
                    {
                        record.Rewind();

                        if (CurrentState == State.Recording && !record.IsEmpty())
                        {
                            cassette.Save(record);
                        }

                        record = null;
                    }

                    httpListener.Close();

                    CurrentState = State.Off;
                }
            }
        }
    }
}
