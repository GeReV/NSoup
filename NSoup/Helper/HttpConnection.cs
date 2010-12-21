using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using NSoup.Parse;
using NSoup.Nodes;
using System.Net;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace NSoup.Helper
{
    public class HttpConnection : IConnection
    {
        #region IConnection Members

        public static IConnection Connect(string url)
        {
            IConnection con = new HttpConnection();
            con.Url(url);
            return con;
        }

        public static IConnection Connect(Uri url)
        {
            IConnection con = new HttpConnection();
            con.Url(url);
            return con;
        }

        private IRequest req;
        private IResponse res;

        private HttpConnection()
        {
            req = new Request();
            res = new Response();
        }

        public IConnection Url(Uri url)
        {
            req.Url(url);
            return this;
        }

        public IConnection Url(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Must supply a valid URL", "url");
            }
            try
            {
                req.Url(new Uri(url));
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException("Malformed URL: " + url, e);
            }
            return this;
        }

        public IConnection UserAgent(string userAgent)
        {
            if (userAgent == null)
            {
                throw new ArgumentNullException("userAgent");
            }

            req.Header("User-Agent", userAgent);

            return this;
        }

        public IConnection Timeout(int millis)
        {
            req.Timeout(millis);

            return this;
        }

        public IConnection Referrer(string referrer)
        {
            if (referrer == null)
            {
                throw new ArgumentNullException("referrer");
            }

            req.Header("Referer", referrer); // Note "Referer" is the actual header spelling.

            return this;
        }

        public IConnection Method(Method method)
        {
            req.Method(method);

            return this;
        }

        public IConnection Data(string key, string value)
        {
            req.Data(KeyVal.Create(key, value));

            return this;
        }

        public IConnection Data(IDictionary<string, string> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            foreach (KeyValuePair<string, string> entry in data)
            {
                req.Data(KeyVal.Create(entry.Key, entry.Value));
            }

            return this;
        }

        public IConnection Data(params string[] keyvals)
        {
            if (keyvals == null)
            {
                throw new ArgumentNullException("keyvals");
            }

            if ((keyvals.Length % 2) != 0)
            {
                throw new InvalidOperationException("Must supply an even number of key value pairs");
            }

            for (int i = 0; i < keyvals.Length; i += 2)
            {
                string key = keyvals[i];
                string value = keyvals[i + 1];

                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Data key must not be empty");
                }

                if (value == null)
                {
                    throw new ArgumentException("Data value must not be null");
                }

                req.Data(KeyVal.Create(key, value));
            }

            return this;
        }

        public IConnection Header(string name, string value)
        {
            req.Header(name, value);

            return this;
        }

        public IConnection Cookie(string name, string value)
        {
            req.Cookie(name, value);

            return this;
        }

        public Document Get()
        {
            req.Method(NSoup.Method.Get);

            Execute();

            return res.Parse();
        }

        public Document Post()
        {
            req.Method(NSoup.Method.Post);

            Execute();

            return res.Parse();
        }

        public IResponse Execute()
        {
            res = NSoup.Helper.Response.Execute(req);

            return res;
        }

        public IRequest Request()
        {
            return req;
        }

        public IConnection Request(IRequest request)
        {
            req = request;

            return this;
        }

        public IResponse Response()
        {
            return res;
        }

        public IConnection Response(IResponse response)
        {
            res = response;

            return this;
        }

        #endregion
    }

    public abstract class ConnectionBase<T> : IConnectionBase<T> where T : IConnectionBase<T>
    {
        protected Uri _url;
        protected Method _method;
        protected IDictionary<string, string> _headers;
        protected IDictionary<string, string> _cookies;

        protected ConnectionBase()
        {
            _headers = new SortedDictionary<string, string>();
            _cookies = new SortedDictionary<string, string>();
        }

        public Uri Url()
        {
            return _url;
        }

        public IConnectionBase<T> Url(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException("url");
            }

            this._url = url;
            return (IConnectionBase<T>)this;
        }

        public Method Method()
        {
            return _method;
        }

        public IConnectionBase<T> Method(Method method)
        {
            /*if (method == null)
            {
                throw new ArgumentNullException("method");
            }*/

            this._method = method;
            return (IConnectionBase<T>)this;
        }

        public string Header(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            return _headers[name];
        }

        public IConnectionBase<T> Header(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Header name must not be empty", "name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            _headers[name] = value;

            return (IConnectionBase<T>)this;
        }

        public bool HasHeader(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Header name must not be empty", "name");
            }

            return _headers.ContainsKey(name);
        }

        public IConnectionBase<T> RemoveHeader(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Header name must not be empty", "name");
            }

            _headers.Remove(name);

            return (IConnectionBase<T>)this;
        }

        public IDictionary<string, string> Headers()
        {
            return _headers;
        }

        public string Cookie(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            return _cookies[name];
        }

        public IConnectionBase<T> Cookie(string name, string value)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Cookie name must not be empty", "name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            _cookies[name] = value;

            return (IConnectionBase<T>)this;
        }

        public bool HasCookie(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Cookie name must not be empty", "name");
            }

            return _cookies.ContainsKey(name);
        }

        public IConnectionBase<T> RemoveCookie(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Cookie name must not be empty", "name");
            }

            _cookies.Remove(name);

            return (IConnectionBase<T>)this;
        }

        public IDictionary<string, string> Cookies()
        {
            return _cookies;
        }
    }

    public class Response : ConnectionBase<IResponse>, IResponse
    {
        private static readonly Regex _charsetPattern = new Regex("(?i)\\bcharset=([^\\s;]*)", RegexOptions.Compiled);

        private HttpStatusCode _statusCode;
        private string _statusMessage;
        private byte[] _byteData;
        private string _charset;
        private string _contentType;
        private bool _executed = false;

        public static Response Execute(IRequest req)
        {

            if (req == null)
            {
                throw new ArgumentNullException("req", "Request must not be null");
            }
            Uri url = req.Url();
            string protocol = url.Scheme;

            if (!protocol.Equals("http") && !protocol.Equals("https"))
            {
                throw new InvalidOperationException("Only http & https protocols supported");
            }

            // set up the request for execution
            if (req.Method() == NSoup.Method.Get && req.Data().Count > 0)
            {
                url = GetRequestUrl(req); // appends query string
            }

            HttpWebRequest conn = (HttpWebRequest)HttpWebRequest.Create(url);

            conn.Method = req.Method().ToString();
            conn.AllowAutoRedirect = true;
            conn.Timeout = req.Timeout();
            conn.ReadWriteTimeout = req.Timeout();

            /*if (req.Method() == Method.Post)
                conn.setDoOutput(true);*/

            if (req.Cookies().Count > 0)
            {
                conn.Headers.Add(HttpRequestHeader.Cookie, GetRequestCookieString(req));
            }

            // Added due to incosistent behavior by .NET when trying to add this header.
            if (req.HasHeader("Referer"))
            {
                conn.Referer = req.Header("Referer");
                
                req.RemoveHeader("Referer");
            }

            // Same as above.
            if (req.HasHeader("User-Agent"))
            {
                conn.UserAgent = req.Header("User-Agent");

                req.RemoveHeader("User-Agent");
            }

            foreach (KeyValuePair<string, string> header in req.Headers())
            {
                conn.Headers.Add(header.Key, header.Value);
            }

            if (req.Method() == NSoup.Method.Post)
            {
                conn.ContentType = "application/x-www-form-urlencoded";
                WritePost(req.Data(), conn.GetRequestStream());
            }

            HttpWebResponse response = (HttpWebResponse)conn.GetResponse();

            // todo: error handling options, allow user to get !200 without exception
            HttpStatusCode status = response.StatusCode;
            if (status != HttpStatusCode.OK)
            {
                throw new IOException(status + " error loading URL " + url.ToString());
            }

            Response res = new Response();
            res.SetupFromConnection(response);

            using (Stream inStream =
                (res.HasHeader("Content-Encoding") && res.Header("Content-Encoding").Equals("gzip")) ?
                    new GZipStream(response.GetResponseStream(), CompressionMode.Decompress) :
                    response.GetResponseStream())
            {
                res._byteData = DataUtil.ReadToByteBuffer(inStream);
                res._charset = GetCharsetFromContentType(res.ContentType()); // may be null, readInputStream deals with it
            }

            res._executed = true;
            return res;
        }

        public HttpStatusCode StatusCode()
        {
            return _statusCode;
        }

        public string StatusMessage()
        {
            return _statusMessage;
        }

        public string Charset()
        {
            return _charset;
        }

        public string ContentType()
        {
            return _contentType;
        }

        public Document Parse()
        {
            if (!_executed)
            {
                throw new InvalidOperationException("Request must be executed (with .Execute(), .Get(), or .Post() before parsing response ");
            }

            if (_contentType == null || !_contentType.StartsWith("text/"))
            {
                throw new IOException(string.Format("Unhandled content type \"{0}\" on URL {1}. Must be text/*",
                    _contentType, _url.ToString()));
            }
            Document doc = DataUtil.ParseByteData(_byteData, _charset, _url.ToString());

            _charset = doc.Settings.Encoding.WebName.ToUpperInvariant(); // update charset from meta-equiv, possibly
            return doc;
        }

        public string Body()
        {
            if (!_executed)
            {
                throw new InvalidOperationException("Request must be executed (with .Execute(), .Get(), or .Post() before getting response body");
            }

            // charset gets set from header on execute, and from meta-equiv on parse. parse may not have happened yet
            string body;
            if (_charset == null)
            {
                body = DataUtil.DefaultEncoding.GetString(_byteData);
            }
            else
            {
                body = Encoding.GetEncoding(_charset).GetString(_byteData);
            }

            return body;
        }

        public byte[] BodyAsBytes()
        {
            if (!_executed)
            {
                throw new InvalidOperationException("Request must be executed (with .Execute(), .Get(), or .Post() before getting response body");
            }
            return _byteData;
        }

        // set up url, method, header, cookies
        private void SetupFromConnection(HttpWebResponse conn)
        {

            _method = (Method)Enum.Parse(typeof(Method), conn.Method, true);

            _url = conn.ResponseUri;
            _statusCode = conn.StatusCode;
            _statusMessage = conn.StatusDescription;
            _contentType = conn.ContentType;

            WebHeaderCollection resHeaders = conn.Headers;
            foreach (string name in resHeaders.Keys)
            {
                if (name == null)
                {
                    continue; // http/1.1 line
                }

                string[] values = resHeaders[name].Split(';');

                if (name.Equals("Set-Cookie"))
                {
                    foreach (string value in values)
                    {
                        TokenQueue cd = new TokenQueue(value);
                        string cookieName = cd.ChompTo("=").Trim();
                        string cookieVal = cd.ConsumeTo(";").Trim();
                        // ignores path, date, domain, secure et al. req'd?
                        Cookie(cookieName, cookieVal);
                    }
                }
                else
                { // only take the first instance of each header
                    Header(name, values[0]);
                }
            }
        }

        private static void WritePost(ICollection<KeyVal> data, Stream outputStream)
        {
            StringBuilder sb = new StringBuilder();

            bool first = true;
            foreach (IKeyVal keyVal in data)
            {
                if (!first)
                {
                    sb.Append('&');
                }
                else
                {
                    first = false;
                }

                sb.Append(HttpUtility.UrlEncode(keyVal.Key(), DataUtil.DefaultEncoding))
                    .Append('=')
                    .Append(HttpUtility.UrlEncode(keyVal.Value(), DataUtil.DefaultEncoding));
            }

            byte[] bytes = DataUtil.DefaultEncoding.GetBytes(sb.ToString());

            outputStream.Write(bytes, 0, bytes.Length);
            outputStream.Close();
        }

        private static string GetRequestCookieString(IRequest req)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (KeyValuePair<string, string> cookie in req.Cookies())
            {
                if (!first)
                {
                    sb.Append("; ");
                }
                else
                {
                    first = false;
                }
                sb.Append(cookie.Key).Append('=').Append(cookie.Value);
                // todo: spec says only ascii, no escaping / encoding defined. validate on set? or escape somehow here?
            }
            return sb.ToString();
        }

        private static Uri GetRequestUrl(IRequest req)
        {
            Uri input = req.Url();
            StringBuilder url = new StringBuilder();
            bool first = true;
            // reconstitute the query, ready for appends
            url
                .Append(input.Scheme)
                .Append("://")
                .Append(input.Authority) // includes host, port
                .Append(input.AbsolutePath);
                //.Append("?");

            if (!string.IsNullOrEmpty(input.Query))
            {
                url.Append(input.Query);
                first = false;
            }

            foreach (IKeyVal keyVal in req.Data())
            {
                if (!first)
                {
                    url.Append('&');
                }
                else
                {
                    first = false;
                }

                url
                    .Append(HttpUtility.UrlEncode(keyVal.Key(), DataUtil.DefaultEncoding))
                    .Append('=')
                    .Append(HttpUtility.UrlEncode(keyVal.Value(), DataUtil.DefaultEncoding));
            }
            return new Uri(url.ToString());
        }

        /// <summary>
        /// Parse out a charset from a content type header.
        /// </summary>
        /// <param name="contentType">e.g. "text/html; charset=EUC-JP"</param>
        /// <returns>"EUC-JP", or null if not found. Charset is trimmed and uppercased.</returns>
        private static string GetCharsetFromContentType(string contentType)
        {
            if (contentType == null) return null;

            Match m = _charsetPattern.Match(contentType);
            if (m.Success)
            {
                return m.Groups[1].Value.Trim().ToUpperInvariant();
            }
            return null;
        }
    }

    public class Request : ConnectionBase<IRequest>, IRequest
    {
        private int _timeoutMilliseconds;
        private ICollection<KeyVal> _data;

        public Request()
        {
            _timeoutMilliseconds = 3000;
            _data = new List<KeyVal>();
            _method = NSoup.Method.Get;
            _headers["Accept-Encoding"] = "gzip";
        }

        public int Timeout()
        {
            return _timeoutMilliseconds;
        }

        public IRequest Timeout(int millis)
        {
            if (millis < 0)
            {
                throw new ArgumentOutOfRangeException("Timeout milliseconds must be 0 (infinite) or greater");
            }

            this._timeoutMilliseconds = millis;

            return this;
        }

        public IRequest Data(KeyVal keyval)
        {
            if (keyval == null)
            {
                throw new ArgumentNullException("keyval");
            }

            _data.Add(keyval);

            return this;
        }

        public ICollection<KeyVal> Data()
        {
            return _data;
        }
    }

    public class KeyVal : IKeyVal
    {
        private string _key;
        private string _value;

        public static KeyVal Create(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Data key must not be empty", "key");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value", "Data value must not be null");
            }

            return new KeyVal(key, value);
        }

        private KeyVal(string key, string value)
        {
            this._key = key;
            this._value = value;
        }

        #region IKeyVal Members

        public IKeyVal Key(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Data key must not be empty", "key");
            }

            this._key = key;

            return this;
        }

        public string Key()
        {
            return _key;
        }

        public IKeyVal Value(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value", "Data value must not be null");
            }

            this._value = value;

            return this;
        }

        public string Value()
        {
            return _value;
        }

        #endregion

        public override string ToString()
        {
            return string.Concat(_key, "=", _value);
        }
    }
}