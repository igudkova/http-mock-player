using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace HttpMockPlayer.Tests
{
    public class Client
    {
        private Uri baseAddress;

        public Client(Uri baseAddress)
        {
            this.baseAddress = baseAddress;
        }

        public WebResponse Send(string path, string method, string content = null, NameValueCollection headers = null, CookieCollection cookies = null)
        {
            var request = WebRequest.CreateHttp(new Uri(baseAddress, path));
            request.Method = method;

            if (headers != null)
            {
                foreach (string header in headers)
                {
                    request.Headers[header] = headers[header];
                }
            }

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            if (content != null)
            {
                byte[] data = Encoding.Default.GetBytes(content);

                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
           }

            return request.GetResponse();
        }
    }
}
