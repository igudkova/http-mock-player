using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HttpMockReq
{
    class Player
    {
        private bool isPlaying;
        private bool isRecording;
        private string remoteUrl;
        private HttpListener httpListener;
        private HttpClient httpClient;
        private Queue queue;
        private Cassette cassette;

        public void On(string url = "http://localhost:5555")
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add(url.EndsWith("/") ? url : url + "/");
            httpListener.Start();

            Task.Factory.StartNew(() =>
            {
                while (httpListener.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = httpListener.GetContext();
                        HttpListenerRequest request = context.Request;
                        HttpListenerResponse response = context.Response;

                        if(isPlaying)
                        {
                            dynamic rec = queue.Dequeue();

                            if (!VerifyRequestAgainstRecorded(request, rec.request))
                            {
                                throw new InvalidOperationException(string.Format("Request {0} is not found.", request.Url));
                            }
                            BuildResponseFromRecorded(response, rec.response);
                        }
                        else if(isRecording)
                        {
                            //redirect to live url
                            //HttpResponseMessage res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, requestUri));

                            //create json from request and response
                            //add to queue
                        }

                    }
                    catch (System.Exception)
                    {
                        Off();

                        throw;
                    }
                }
            });
        }

        public void Off()
        {
            this.Stop();

            if (httpListener != null)
            {
                httpListener.Stop();
                httpListener.Close();
            }
        }

        public void Connect(string remoteUrl)
        {
            this.remoteUrl = remoteUrl;

            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(remoteUrl);

        }

        public void Disconnect()
        {
            httpClient.Dispose();
            httpClient = null;

            remoteUrl = null;
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

        public void Play(Record record)
        {
            if (isPlaying)
            {
                throw new InvalidOperationException("Player is already playing.");
            }
            if (isRecording)
            {
                throw new InvalidOperationException("Player is already recording.");
            }
            if (cassette == null)
            {
                throw new InvalidOperationException("Cassette is not loaded.");
            }

            isPlaying = true;

            queue = record.Queue;
        }

        #endregion

        #region Record

        public void Record(string name)
        {
        }

        #endregion

        public void Pause()
        {

        }

        public void Stop()
        {
            if (isPlaying)
            {
                isPlaying = false;

                queue = null;
            }
            else if (isRecording)
            {
                //save to file
                //cassette.Save();
            }
        }
    }
}
