using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NSoup
{
    /// <summary>
    /// Signals that a HTTP response returned a mime type that is not supported.
    /// </summary>
    public class UnsupportedMimeTypeException : IOException
    {
        private string _mimeType;
        private string _url;

        public UnsupportedMimeTypeException(string message, string mimeType, string url)
            : base(message)
        {
            this._mimeType = mimeType;
            this._url = url;
        }

        public string MimeType
        {
            get { return _mimeType; }
        }

        public string Url
        {
            get { return _url; }
        }

        public override string ToString()
        {
            return base.ToString() + ". Mimetype=" + MimeType + ", URL=" + Url;
        }
    }
}
