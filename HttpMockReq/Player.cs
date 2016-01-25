using HttpMockReq.HttpMockReqException;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

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
        private Queue queue;
        private Cassette cassette;

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
                return baseAddress;
            }
            set
            {
                if (baseAddress != value)
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
                throw new InvalidOperationException("Base address is not set.");
            }

            httpListener.Start();

            Task.Factory.StartNew(async () =>
            {
                while (httpListener.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = httpListener.GetContext();
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        if (state == State.Playing)
                        {
                            dynamic rec = queue.Dequeue();

                            if (!VerifyRequestAgainstRecorded(request, rec.request))
                            {
                                throw new RequestNotFoundException("The processed request doesn't match the recorded one.", request.Url);
                            }
                            BuildResponseFromRecorded(response, rec.response);
                        }
                        else if (state == State.Recording)
                        {
                            var requestMessage = new HttpRequestMessage(new HttpMethod(request.HttpMethod), request.Url.PathAndQuery);

                            foreach (string header in request.Headers.AllKeys)
                            {
                                requestMessage.Headers.Add(header, request.Headers[header]);
                            }
                            requestMessage.Headers.Host = remoteAddress.Host;


                            //foreach (string cookie in request.Cookies)
                            //{
                            //    requestMessage.Properties.Add(cookie, request.Cookies[cookie]);
                            //}

                            HttpResponseMessage res = await httpClient.SendAsync(requestMessage);
                            string resString = await res.Content.ReadAsStringAsync();
                            //create json from request and response, incl. baseurl
                            //add to queue

                            Console.WriteLine("recording");

                            httpClient.DefaultRequestHeaders.Clear();
                        }
                    }
                    catch (Exception ex)
                    {
                        Stop();

                        throw new OperationFailedException(state, "Player couldn't complete the operation.", ex);
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
                httpListener.Stop();
                httpListener.Close();
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

        private bool VerifyRequestAgainstRecorded(HttpListenerRequest request, dynamic recordedRequest)
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

        private void BuildResponseFromRecorded(HttpListenerResponse response, dynamic recordedResponse)
        {
            //todo
            response.StatusCode = recordedResponse.status;

            if (recordedResponse.headers != null)
            {
                foreach (var header in recordedResponse.headers)
                {
                    response.Headers.Add(header.Name, header.Value.ToString());
                }
            }

            byte[] responseBuffer = Encoding.UTF8.GetBytes(recordedResponse.body.ToString()); //?
            response.ContentLength64 = responseBuffer.Length;

            Stream responseStream = response.OutputStream;
            responseStream.Write(responseBuffer, 0, responseBuffer.Length);
            responseStream.Close();
        }

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

            var record = cassette.Records.Find(r => r.Name == name);
            queue = record.Queue;

            state = State.Playing;
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

            //if remoteaddr null
            //if httpclient null = diconnected

            state = State.Recording;
        }

        #endregion

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
                queue = null;
            }
            else if (state == State.Recording)
            {
                //save to file
                //cassette.Save();
            }

            state = State.Idle;
        }
    }
}
