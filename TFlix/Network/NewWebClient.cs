using System;
using System.Net;

namespace TFlix.Network
{
    class NewWebClient : WebClient
    {
        private long from;
        private long to;

        public void AddRange(long fr, long t)
        {
            from = fr;
            to = t;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);
            request.AddRange(this.from, this.to);
            return request;
        }

    }
}