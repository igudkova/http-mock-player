using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpMockReq.HttpMockReqException;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Net.Http.Headers;

namespace HttpMockReq
{
    /// <summary>
    /// 
    /// </summary>
    public class Player
    {
        private Uri baseAddress, remoteAddress;
        private HttpListener httpListener;
        private HttpClient httpClient;
        private State state;
        private Cassette cassette;
        private Record record;

        private object statelock = new object();

        private class MockRequest
        {
            private MockRequest() { }

            internal string Method { get; private set; }

            internal string Path { get; private set; }

            internal string Body { get; private set; }

            internal NameValueCollection Headers { get; private set; }

            internal CookieCollection Cookies { get; private set; }

            internal static MockRequest FromHttpRequest(string host, HttpListenerRequest request)
            {
                var mockRequest = new MockRequest()
                {
                    Method = request.HttpMethod,
                    Path = request.Url.PathAndQuery,
                    Headers = request.Headers,
                    Cookies = request.Cookies
                };

                mockRequest.Headers.Set("Host", host);

                if(request.HasEntityBody)
                {
                    //Stream body = request.InputStream;
                    //Encoding encoding = request.ContentEncoding;
                    //StreamReader reader = new StreamReader(body, encoding);

                    //if (request.ContentType != null)
                    //{
                    //    Console.WriteLine("Client data content type {0}", request.ContentType);
                    //}
                    //Console.WriteLine("Client data content length {0}", request.ContentLength64);

                    //Console.WriteLine("Start of client data:");

                    //Body = reader.ReadToEnd();

                    //body.Close();
                    //reader.Close();
                }

                return mockRequest;
            }

            internal static MockRequest FromRecordedRequest(JObject recorded)
            {
                return recorded.ToObject<MockRequest>();
            }
        }

        private class MockResponse
        {
            private MockResponse() { }

            internal int StatusCode { get; private set;  }

            internal string StatusDescription { get; private set; }

            internal string Body { get; private set; }

            internal HttpResponseHeaders Headers { get; private set; }

            internal static async Task<MockResponse> FromHttpResponse(HttpResponseMessage responseMessage)
            {
                return new MockResponse()
                {
                    StatusCode = (int)responseMessage.StatusCode,
                    StatusDescription = responseMessage.ReasonPhrase,
                    Body = await responseMessage.Content.ReadAsStringAsync(),
                    Headers = responseMessage.Headers
                };
            }

            internal static MockResponse FromRecordedResponse(JObject recorded)
            {
                return recorded.ToObject<MockResponse>();
            }

            internal static MockResponse FromPlayerError(int statusCode, string statusDescription, string message)
            {
                return new MockResponse()
                {
                    StatusCode = statusCode,
                    StatusDescription = statusDescription,
                    Body = message
                };
            }
        }

        /// <summary>
        /// Represents the state of the player.
        /// </summary>
        public enum State
        {
            Idle,
            Playing,
            Recording
        }

        /// <summary>
        /// Gets or sets the base address 
        /// </summary>
        public Uri BaseAddress
        {
            get
            {
                return baseAddress; // todo: set in constructor only, deck address
            }
            set
            {
                if (baseAddress != value) //todo
                {
                    baseAddress = value;

                    if(httpListener == null)
                    {
                        httpListener = new HttpListener();
                    }

                    var baseAddressString = baseAddress.OriginalString;
                    httpListener.Prefixes.Clear();
                    httpListener.Prefixes.Add(baseAddressString.EndsWith("/") ? baseAddressString : baseAddressString + "/");
                }
            }
        }

        public Uri RemoteAddress 
        {
            get
            {
                return remoteAddress;
            }
            set
            {
                if(remoteAddress != value)
                {
                    remoteAddress = value;

                    if(httpClient == null)
                    {
                        httpClient = new HttpClient();
                    }

                    httpClient.BaseAddress = remoteAddress;
                }
            }
        }

        public void Start()
        {
            if(baseAddress == null)
            {
                throw new InvalidOperationException("Player base address is not set.");
            }

            httpListener.Start();

            Task.Run(async () =>
            {
                while (httpListener.IsListening)
                {
                    HttpListenerContext context = httpListener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    try
                    {
                        switch (state)
                        {
                            case State.Playing:
                                {
                                    var mock = (JObject)record.Read();

                                    if (VerifyRequest(request, MockRequest.FromRecordedRequest(mock)))
                                    {
                                        BuildResponse(response, MockResponse.FromRecordedResponse(mock));
                                    }
                                    else
                                    {
                                        BuildResponse(response, MockResponse.FromPlayerError(454, "Player request mismatch", $"Player could not play the request at {request.Url}. The request doesn't match the current recorded one."));
                                    }
                                }
                                
                                break;
                            case State.Recording:
                                {
                                    var mockRequest = MockRequest.FromHttpRequest(remoteAddress.Host, request);
                                    var requestMessage = BuildRequestMessage(mockRequest);

                                    var responseMessage = await httpClient.SendAsync(requestMessage);
                                    var mockResponse = await MockResponse.FromHttpResponse(responseMessage);

                                    var mock = JObject.FromObject(new
                                    {
                                        request = "",//mockRequest,
                                        response = ""//mockResponse
                                    });

                                    record.Write(mock);

                                    BuildResponse(response, mockResponse);
                                }

                                break;
                            default:
                                throw new OperationFailedException(state, "Player is not in operation.");
                        }
                    }
                    catch (Exception ex)
                    {
                        int statusCode;
                        string process;

                        switch (state)
                        {
                            case State.Playing:
                                statusCode = 551;
                                process = "play";

                                break;
                            case State.Recording:
                                statusCode = 552;
                                process = "record";

                                break;
                            default:
                                statusCode = 550;
                                process = "process";

                                break;
                        }

                        BuildResponse(response, MockResponse.FromPlayerError(statusCode, "Player exception", $"Player could not {process} the request at {request.Url} because of exception. {ex}"));
                    }
                    finally
                    {
                        response.Close();
                    }
                }
            });
        }

        /// <summary>
        /// Shuts down the <see cref="Player"/>.
        /// </summary>
        public void Close()
        {
            if (httpListener != null)
            {
                //httpListener.Stop();
                httpListener.Close(); //dispose and =null?
            }

            if(httpClient != null)
            {
                httpClient.Dispose();
                httpClient = null;
            }
        }

        public void Load(Cassette cassette)
        {
            this.cassette = cassette;
        }

        #region Play

        public void Play(string name)
        {
            lock (statelock)
            {
                if (state != State.Idle)
                {
                    throw new OperationFailedException(state, "Player is already in operation.");
                }
                if (cassette == null)
                {
                    throw new InvalidOperationException("Cassette is not loaded.");
                }

                record = cassette.Find(name);

                if (record == null)
                {
                    throw new InvalidOperationException($"Cassette doesn't contain a record with the given name: {name}.");
                }

                state = State.Playing;
            }
        }

        private bool VerifyRequest(HttpListenerRequest request, MockRequest mockRequest)
        {
            ////todo
            //bool isMatching = mockRequest.Method == request.HttpMethod &&
            //                  mockRequest.Path == request.Url.AbsolutePath &&
            //                  (mockRequest.query ?? "") == request.Url.Query;
            //if (isMatching && mockRequest.headers != null)
            //{
            //    foreach (var header in mockRequest.headers)
            //    {
            //        if (request.Headers.Get(header.Name) != header.Value.ToString())
            //        {
            //            isMatching = false;
            //            break;
            //        }
            //    }
            //}

            //return isMatching;
            return true;
        }

        #endregion

        #region Record

        public void Record(string name)
        {
            lock (statelock)
            {
                if (state != State.Idle)
                {
                    throw new OperationFailedException(state, "Player is already in operation.");
                }
                if (cassette == null)
                {
                    throw new InvalidOperationException("Cassette is not loaded.");
                }

                record = new Record(name);

                state = State.Recording;

                //if remoteaddr null
                //if httpclient null = diconnected
            }
        }

        private HttpRequestMessage BuildRequestMessage(MockRequest mockRequest)
        {
            var requestMessage = new HttpRequestMessage()
            {
                Method = new HttpMethod(mockRequest.Method),
                RequestUri = new Uri(mockRequest.Path, UriKind.Relative)
            };

            if(mockRequest.Body != null)
            {
                requestMessage.Content = new StringContent(mockRequest.Body);
            }

            foreach (string header in mockRequest.Headers.Keys)
            {
                requestMessage.Headers.Add(header, mockRequest.Headers[header]);
            }

            //foreach (string cookie in request.Cookies)
            //{
            //    requestMessage.Properties.Add(cookie, request.Cookies[cookie]);
            //}

            return requestMessage;
        }

        #endregion

        private void BuildResponse(HttpListenerResponse response, MockResponse mockResponse)
        {
            response.StatusCode = mockResponse.StatusCode;
            response.StatusDescription = mockResponse.StatusDescription;

            byte[] responseBuffer = Encoding.UTF8.GetBytes(mockResponse.Body);
            response.ContentLength64 = responseBuffer.Length;

            Stream responseStream = response.OutputStream;
            responseStream.Write(responseBuffer, 0, responseBuffer.Length);
            responseStream.Close();

            if (mockResponse.Headers != null)
            {
                foreach (var header in mockResponse.Headers)
                {
                    response.Headers.Add(header.Key, header.Value.ToString());
                }
            }
        }

        /// <summary>
        /// Causes the player to stop playing or recording requests.
        /// </summary>
        public void Stop()
        {
            lock (statelock)
            {
                record.Rewind();

                if (state == State.Recording)
                {
                    cassette.Save(record);
                }

                record = null;

                state = State.Idle;
            }
        }
    }
}
