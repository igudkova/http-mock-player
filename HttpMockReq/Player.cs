using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using HttpMockReq.HttpMockReqException;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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

        /// <summary>
        /// Represents the state of the player.
        /// </summary>
        public enum State
        {
            Idle,
            Playing,
            Recording
        }

        private class MockRequest
        {
            public string Method { get; private set; }

            public string Host { get; private set; }

            public string Path { get; private set; }

            public string Body { get; private set; }

            public Dictionary<string, string> Headers { get; private set; }

            public Dictionary<string, Cookie> Cookies { get; private set; }

            public static MockRequest FromHttpRequest(string host, HttpListenerRequest request)
            {
                var mockRequest = new MockRequest()
                {
                    Method = request.HttpMethod,
                    Host = host,
                    Path = request.Url.PathAndQuery
                };

                if(request.Headers != null)
                {
                    mockRequest.Headers = new Dictionary<string, string>();

                    foreach (var header in request.Headers.AllKeys)
                    {
                        mockRequest.Headers.Add(header, request.Headers[header]);
                    }
                }

                return mockRequest;

                ////foreach (string cookie in request.Cookies)
                ////{
                ////    requestMessage.Properties.Add(cookie, request.Cookies[cookie]);
                ////}

                //requestMessage.Headers.Host = remoteAddress.Host;

            }

            public static MockRequest FromRecordedRequest(JObject recorded)
            {
                return recorded.ToObject<MockRequest>();
            }
        }

        private class MockResponse
        {
            public int StatusCode { get; private set;  }

            public string StatusDescription { get; private set; }

            public string Body { get; private set; }

            public Dictionary<string, string> Headers { get; private set; }

            public MockResponse(int statusCode, string statusDescription, string body)
            {
                StatusCode = statusCode;
                StatusDescription = statusDescription;
                Body = body;
            }

            public static async Task<MockResponse> FromHttpResponse(HttpResponseMessage responseMessage)
            {
                var mockResponse = new MockResponse((int)responseMessage.StatusCode, responseMessage.ReasonPhrase, await responseMessage.Content.ReadAsStringAsync());

                if (responseMessage.Headers != null)
                {
                    mockResponse.Headers = new Dictionary<string, string>();

                    foreach (var header in responseMessage.Headers)
                    {
                        mockResponse.Headers.Add(header.Key, header.Value.ToString());
                    }
                }

                return mockResponse;
            }

            public static MockResponse FromRecordedResponse(JObject recorded)
            {
                return recorded.ToObject<MockResponse>();
            }
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
                                dynamic rec = record.Read(); //todo: object of MockRequest

                                if (VerifyRequest(request, rec.request))
                                {
                                    BuildResponse(response, rec.response);
                                }
                                else
                                {
                                    BuildResponse(response, new MockResponse(454, "Player request mismatch", $"Player could not play the request at {request.Url}. The request doesn't match the current recorded one."));
                                }
                                
                                break;
                            case State.Recording:
                                var mockRequest = MockRequest.FromHttpRequest(remoteAddress.Host, request);
                                var requestMessage = new HttpRequestMessage();

                                BuildRequestMessage(requestMessage, mockRequest);

                                var responseMessage = await httpClient.SendAsync(requestMessage);
                                var mockResponse = await MockResponse.FromHttpResponse(responseMessage);

                                //create json from request and response, incl. baseurl
                                //add to queue
                                var obj = JToken.FromObject(mockResponse);
                                record.Write(obj);

                                BuildResponse(response, mockResponse);

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

                        BuildResponse(response, new MockResponse(statusCode, "Player exception", $"Player could not {process} the request at {request.Url} because of exception. {ex}"));
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
            this.Stop();

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
            if (state != State.Idle)
            {
                throw new OperationFailedException(state, "Player is already in operation.");
            }
            if (cassette == null)
            {
                throw new InvalidOperationException("Cassette is not loaded.");
            }

            state = State.Playing;

            record = cassette.Records.Find(r => r.Name == name);
        }

        private bool VerifyRequest(HttpListenerRequest request, dynamic recordedRequest)
        {
            //todo
            bool isMatching = recordedRequest.method == request.HttpMethod &&
                              recordedRequest.path == request.Url.AbsolutePath &&
                              (recordedRequest.query ?? "") == request.Url.Query;
            if (isMatching && recordedRequest.headers != null)
            {
                foreach (var header in recordedRequest.headers)
                {
                    if (request.Headers.Get(header.Name) != header.Value.ToString())
                    {
                        isMatching = false;
                        break;
                    }
                }
            }

            return isMatching;
        }

        #endregion

        #region Record

        public void Record(string name)
        {
            if (state != State.Idle)
            {
                throw new OperationFailedException(state, "Player is already in operation.");
            }
            if (cassette == null)
            {
                throw new InvalidOperationException("Cassette is not loaded.");
            }

            state = State.Recording;

            record = new Record(name);
            //if remoteaddr null
            //if httpclient null = diconnected
        }

        private HttpRequestMessage BuildRequestMessage(HttpRequestMessage requestMessage, MockRequest mockRequest)
        {
            requestMessage.Method = new HttpMethod(mockRequest.Method);
            requestMessage.RequestUri = new Uri(mockRequest.Path, UriKind.Relative);

            foreach (string header in mockRequest.Headers.Keys)
            {
                requestMessage.Headers.Add(header, mockRequest.Headers[header]);
            }

            //foreach (string cookie in request.Cookies)
            //{
            //    requestMessage.Properties.Add(cookie, request.Cookies[cookie]);
            //}

            requestMessage.Headers.Host = mockRequest.Host;

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

        public void Pause()
        {

        }

        /// <summary>
        /// Causes the player to stop playing or recording requests.
        /// </summary>
        public void Stop()
        {
            if (state == State.Playing)
            {
            }
            else if (state == State.Recording)
            {
                //save to file
                //cassette.Save();
            }

            state = State.Idle;

            record = null;
        }
    }
}
