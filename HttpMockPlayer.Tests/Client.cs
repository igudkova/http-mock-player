using System;
using System.Net;

namespace HttpMockPlayer.Tests
{
    public class Client
    {
        private Uri baseAddress;

        public Client(Uri baseAddress)
        {
            this.baseAddress = baseAddress;
        }

        public WebResponse Get(string path)
        {
            var request = WebRequest.CreateHttp(new Uri(baseAddress, path));

            request.Method = "GET";

            return request.GetResponse();
        }
    }
}
