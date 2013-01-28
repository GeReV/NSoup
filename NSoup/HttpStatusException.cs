using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NSoup
{
    /// <summary>
    /// Signals that a HTTP request resulted in a not OK HTTP response.
    /// </summary>
    public class HttpStatusException : IOException
    {
        private int _statusCode;
        private string _url;

        public HttpStatusException(string message, int statusCode, string url)
            : base(message)
        {
            this._statusCode = statusCode;
            this._url = url;
        }

        public int StatusCode
        {
            get { return _statusCode; }
        }

        public string Url
        {
            get { return _url; }
        }

        public override string ToString()
        {
            return base.ToString() + ". Status=" + StatusCode + ", URL=" + Url;
        }
    }
}

