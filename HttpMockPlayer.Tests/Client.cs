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
                    var value = headers[header];

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
                            if (value.ToLower() != "100-continue")
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

            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            if (content != null)
            {
                var data = Encoding.UTF8.GetBytes(content);

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
