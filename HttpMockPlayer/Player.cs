using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        // mutex object, used to avoid collisions when processing an incoming request 
        // according to the current player state, or updating the player state value
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

            internal string Path { get; private set; }

            internal string Content { get; private set; }

            internal string ContentEncoding { get; private set; }

            internal string ContentType { get; private set; }

            internal NameValueCollection Headers { get; private set; }

            internal CookieCollection Cookies { get; private set; }

            internal JObject ToJson()
            {
                var jrequest = new JObject();

                jrequest.Add("method", Method);
                jrequest.Add("path", Path);

                if (Content != null)
                {
                    jrequest.Add("content", Content);
                }

                if (ContentEncoding != null)
                {
                    jrequest.Add("contentEncoding", ContentEncoding);
                }

                if(ContentType != null)
                { 
                    jrequest.Add("contentType", ContentType);
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

                if (Cookies != null)
                {
                    var jcookies = new JArray();
                    foreach (Cookie cookie in Cookies)
                    {
                        jcookies.Add(cookie);
                    }
                    jrequest.Add("cookies", jcookies);
                }

                return jrequest;
            }

            internal static MockRequest FromJson(JObject jrequest)
            {
                var mockRequest = new MockRequest()
                {
                    Method = jrequest["method"].ToString(),
                    Path = jrequest["path"].ToString()
                };

                if (jrequest["content"] != null)
                {
                    mockRequest.Content = jrequest["content"].ToString();
                }

                if (jrequest["contentEncoding"] != null)
                {
                    mockRequest.ContentEncoding = jrequest["contentEncoding"].ToString();
                }

                if (jrequest["contentType"] != null)
                { 
                    mockRequest.ContentType = jrequest["contentType"].ToString();
                }

                if (jrequest["headers"] != null)
                {
                    mockRequest.Headers = new NameValueCollection();
                    foreach (JProperty jheader in jrequest["headers"])
                    {
                        mockRequest.Headers.Add(jheader.Name, jheader.Value.ToString());
                    }
                }

                if (jrequest["cookies"] != null)
                {
                    mockRequest.Cookies = new CookieCollection();
                    foreach (JProperty jcookie in jrequest["cookies"])
                    {
                        mockRequest.Cookies.Add(new Cookie(jcookie.Name, jcookie.Value.ToString()));
                    }
                }

                return mockRequest;
            }

            internal static MockRequest FromHttpRequest(string host, HttpListenerRequest request)
            {
                var mockRequest = new MockRequest()
                {
                    Method = request.HttpMethod,
                    Path = request.Url.PathAndQuery
                };

                if (request.HasEntityBody)
                {
                    using (var stream = request.InputStream)
                    using (var reader = new StreamReader(stream, request.ContentEncoding ?? Encoding.UTF8))
                    {
                        mockRequest.Content = reader.ReadToEnd();
                    }
                }

                if (request.ContentEncoding != null)
                {
                    mockRequest.ContentEncoding = request.ContentEncoding.EncodingName;
                }

                if (request.ContentType != null)
                {
                    mockRequest.ContentType = request.ContentType;
                }

                if(request.Headers != null && request.Headers.Count > 0)
                {
                    mockRequest.Headers = request.Headers;

                    if (request.Headers["Host"] != null)
                    {
                        mockRequest.Headers["Host"] = host;
                    }
                }

                if(request.Cookies != null && request.Cookies.Count > 0)
                {
                    mockRequest.Cookies = request.Cookies;
                }

                return mockRequest;
            }

            private bool IsEqual(MockRequest mockRequest)
            {
                if (!string.Equals(Method, mockRequest.Method))
                {
                    return false;
                }

                if(!string.Equals(Path, mockRequest.Path))
                {
                    return false;
                }

                if(!string.Equals(Content, mockRequest.Content))
                {
                    return false;
                }

                if (!string.Equals(ContentEncoding, mockRequest.ContentEncoding))
                {
                    return false;
                }

                if (!string.Equals(ContentType, mockRequest.ContentType))
                {
                    return false;
                }

                if((Headers == null) != (mockRequest.Headers == null))
                {
                    return false;
                }
                if (Headers != null)
                {
                    if (Headers.Count != mockRequest.Headers.Count)
                    {
                        return false;
                    }
                    foreach (string header in Headers)
                    {
                        if (!string.Equals(Headers[header], mockRequest.Headers[header]))
                        {
                            return false;
                        }
                    }
                }

                if ((Cookies == null) != (mockRequest.Cookies == null))
                {
                    return false;
                }
                if(Cookies != null)
                {
                    if (Cookies.Count != mockRequest.Cookies.Count)
                    {
                        return false;
                    }
                    foreach (Cookie cookie in Cookies)
                    {
                        if (!cookie.Equals(mockRequest.Cookies[cookie.Name]))
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
                return Tuple.Create(Method, Path, Content).GetHashCode();
            }
        }

        private class MockResponse
        {
            private MockResponse() { }

            internal int StatusCode { get; private set; }

            internal string StatusDescription { get; private set; }

            internal string Content { get; private set; }

            internal string ContentEncoding { get; private set; }

            internal string ContentType { get; private set; }

            internal WebHeaderCollection Headers { get; private set; }

            internal CookieCollection Cookies { get; private set; }

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

                if(ContentEncoding != null)
                {
                    jresponse.Add("contentEncoding", ContentEncoding);
                }

                if (ContentType != null)
                {
                    jresponse.Add("contentType", ContentType);
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

                if (Cookies != null)
                {
                    var jcookies = new JArray();
                    foreach (Cookie cookie in Cookies)
                    {
                        jcookies.Add(cookie);
                    }
                    jresponse.Add("cookies", jcookies);
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
                    mockResponse.Content = jresponse["content"].ToString();
                }

                if (jresponse["contentEncoding"] != null)
                {
                    mockResponse.ContentEncoding = jresponse["contentEncoding"].ToString();
                }

                if (jresponse["contentType"] != null)
                {
                    mockResponse.ContentType = jresponse["contentType"].ToString();
                }

                if (jresponse["headers"] != null)
                {
                    mockResponse.Headers = new WebHeaderCollection();
                    foreach (JProperty jheader in jresponse["headers"])
                    {
                        mockResponse.Headers.Add(jheader.Name, jheader.Value.ToString());
                    }
                }

                if (jresponse["cookies"] != null)
                {
                    mockResponse.Cookies = new CookieCollection();
                    foreach (JProperty jcookie in jresponse["cookies"])
                    {
                        mockResponse.Cookies.Add(new Cookie(jcookie.Name, jcookie.Value.ToString()));
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

                if (response.ContentLength > 0)
                {
                    using (var stream = response.GetResponseStream())
                    {
                        Encoding contentEncoding;
                        if (string.IsNullOrEmpty(response.ContentEncoding))
                        {
                            contentEncoding = Encoding.UTF8;
                        }
                        else
                        {
                            contentEncoding = Encoding.GetEncoding(response.ContentEncoding);
                        }

                        using (var reader = new StreamReader(stream, contentEncoding))
                        {
                            mockResponse.Content = reader.ReadToEnd();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(response.ContentEncoding))
                {
                    mockResponse.ContentEncoding = response.ContentEncoding;
                }

                if (response.ContentType != null)
                {
                    mockResponse.ContentType = response.ContentType;
                }

                if(response.Headers != null && response.Headers.Count > 0)
                {
                    mockResponse.Headers = response.Headers;
                }

                if(response.Cookies != null && response.Cookies.Count > 0)
                {
                    mockResponse.Cookies = response.Cookies;
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
            var request = WebRequest.CreateHttp(new Uri(remoteAddress, mockRequest.Path));

            request.Method = mockRequest.Method;

            if (mockRequest.Content != null)
            {
                Encoding contentEncoding;
                if(mockRequest.ContentEncoding == null)
                {
                    contentEncoding = Encoding.UTF8;
                }
                else
                {
                    contentEncoding = Encoding.GetEncoding(mockRequest.ContentEncoding);
                }

                byte[] content = contentEncoding.GetBytes(mockRequest.Content);
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(content, 0, content.Length);
                }

                request.ContentLength = content.Length;
            }

            request.ContentType = mockRequest.ContentType;

            if (mockRequest.Headers != null)
            {
                foreach (string header in mockRequest.Headers)
                {
                    string value = mockRequest.Headers[header];

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
                            request.Expect = value;
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
                            request.TransferEncoding = value;
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

            if(mockRequest.Cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(mockRequest.Cookies);
            }

            return request;
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
                    string value = mockResponse.Headers[header];

                    switch (header)
                    {
                        case "Connection":
                            response.KeepAlive = (value.ToLower() == "keep-alive");
                            break;
                        case "Content-Encoding":
                        case "Content-Length":
                        case "Content-Type":
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

            response.Cookies = mockResponse.Cookies;

            response.ContentType = mockResponse.ContentType;

            if (mockResponse.ContentEncoding == null)
            {
                response.ContentEncoding = null;
            }
            else
            {
                response.ContentEncoding = Encoding.GetEncoding(mockResponse.ContentEncoding);
            }

            if (mockResponse.Content == null)
            {
                response.ContentLength64 = 0;
            }
            else
            {
                var contentEncoding = response.ContentEncoding ?? Encoding.UTF8;
                var content = contentEncoding.GetBytes(mockResponse.Content);

                response.ContentLength64 = content.Length;

                using (var stream = response.OutputStream)
                {
                    // writing to the output stream causes the response be submitted,
                    // i.e. not accepting any further property changes
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
                        HttpListenerContext context = httpListener.GetContext();
                        HttpListenerRequest playerRequest = context.Request;
                        HttpListenerResponse playerResponse = context.Response;

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
                                            var mockPlayerRequest = MockRequest.FromHttpRequest(remoteAddress.Host, playerRequest);

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
                                            var mockRequest = MockRequest.FromHttpRequest(remoteAddress.Host, playerRequest);
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

                    if (CurrentState == State.Recording)
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
                if (httpListener != null)
                {
                    httpListener.Close();
                }

                CurrentState = State.Off;
            }
        }
    }
}
