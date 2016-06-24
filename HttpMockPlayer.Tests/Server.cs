using System;
using System.Net;
using System.Text;

namespace HttpMockPlayer.Tests
{
    public class Server : IDisposable
    {
        private HttpListener httpListener;

        public Server(Uri baseAddress)
        {
            var baseAddressString = baseAddress.OriginalString;

            httpListener = new HttpListener();
            httpListener.Prefixes.Add(baseAddressString.EndsWith("/") ? baseAddressString : baseAddressString + "/");
            httpListener.Start();
        }

        public void Accept()
        {
            var context = httpListener.GetContext();
            var request = context.Request;
            var response = context.Response;

            response.StatusCode = 200;
            response.StatusDescription = "Hurrah!";

            response.KeepAlive = false;
            response.ContentLength64 = 8;
            response.ContentType = "text/plain; charset=ascii";
            response.RedirectLocation = "http://test.com";
            response.Headers["Content-Encoding"] = Encoding.ASCII.WebName;
            response.Headers["Custom-Header"] = "value";

            response.Cookies = new CookieCollection();
            response.Cookies.Add(new Cookie("cookie1", "value1"));
            response.Cookies.Add(new Cookie("cookie2", "value2"));

            var content = Encoding.ASCII.GetBytes("response");
            using (var stream = response.OutputStream)
            {
                stream.Write(content, 0, content.Length);
            }

            response.Close();
        }

        public void Accept(Action<HttpListenerRequest, HttpListenerResponse> callback)
        {
            var context = httpListener.GetContext();
            var request = context.Request;
            var response = context.Response;

            try
            {
                callback(request, response);
            }
            finally
            {
                response.Close();
            }
        }

        public void Reject(Action<HttpListenerRequest, HttpListenerResponse> callback)
        {
            var context = httpListener.GetContext();
            var request = context.Request;
            var response = context.Response;

            response.StatusCode = 400;
            response.StatusDescription = "Wrong";

            try
            {
                callback(request, response);
            }
            finally
            {
                response.Close();
            }
        }

        public void Dispose()
        {
            httpListener.Close();
        }
    }
}
