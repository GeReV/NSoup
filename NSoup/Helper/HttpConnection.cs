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

        public IConnection FollowRedirects(bool followRedirects)
        {
            req.FollowRedirects(followRedirects);

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

        public IConnection IgnoreHttpErrors(bool ignoreHttpErrors)
        {
            req.IgnoreHttpErrors(ignoreHttpErrors);

            return this;
        }

        public IConnection IgnoreContentType(bool ignoreContentType)
        {
            req.IgnoreContentType(ignoreContentType);

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

        private class CaseInsensitiveComparer : IComparer<string>
        {
            #region IComparer<string> Members

            public int Compare(string x, string y)
            {
                return string.Compare(x, y, true);
            }

            #endregion
        }

        protected ConnectionBase()
        {
            _headers = new SortedDictionary<string, string>(new CaseInsensitiveComparer());
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

            return GetHeaderCaseInsensitive(name);
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

            RemoveHeader(name); // ensures we don't get an "accept-encoding" and a "Accept-Encoding"

            _headers[name] = value;

            return (IConnectionBase<T>)this;
        }

        public bool HasHeader(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Header name must not be empty", "name");
            }

            return GetHeaderCaseInsensitive(name) != null;
        }

        public IConnectionBase<T> RemoveHeader(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Header name must not be empty", "name");
            }

            
            KeyValuePair<string, string>? entry = ScanHeaders(name); // remove is case insensitive too
            if (entry != null)
            {
                _headers.Remove(entry.Value.Key); // ensures correct case
            }

            return (IConnectionBase<T>)this;
        }

        public IDictionary<string, string> Headers()
        {
            return _headers;
        }

        private string GetHeaderCaseInsensitive(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name", "Header name must not be null");
            }
            
            // quick evals for common case of title case, lower case, then scan for mixed
            string value = null;

            if (!_headers.TryGetValue(name, out value)) // Also case insensitive thanks to the CaseInsensitiveComparer.
            {
                KeyValuePair<string, string>? entry = ScanHeaders(name);
                if (entry != null)
                {
                    value = entry.Value.Value;
                }
            }

            return value;
        }

        private KeyValuePair<string, string>? ScanHeaders(string name) {
            string lc = name.ToLowerInvariant();
            foreach (KeyValuePair<string, string> entry in _headers) {
                if (entry.Key.ToLowerInvariant().Equals(lc))
                {
                    return entry;
                }
            }
            return null;
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
        private static readonly int MAX_REDIRECTS = 20;
        private HttpStatusCode _statusCode;
        private string _statusMessage;
        private byte[] _byteData;
        private string _charset;
        private string _contentType;
        private bool _executed = false;
        private int _numRedirects = 0;
        private IRequest req;

        public Response()
            : base()
        { }

        private Response(IResponse previousResponse)
            : base()
        {
            if (previousResponse != null)
            {
                _numRedirects = previousResponse.NumRedirects + 1;
                if (_numRedirects >= MAX_REDIRECTS)
                {
                    throw new IOException(string.Format("Too many redirects occurred trying to load URL {0}", previousResponse.Url()));
                }
            }
        }

        public static Response Execute(IRequest req)
        {
            return Execute(req, null);
        }

        public static Response Execute(IRequest req, IResponse previousResponse)
        {
            if (req == null)
            {
                throw new ArgumentNullException("req", "Request must not be null");
            }
            
            string protocol = req.Url().Scheme;

            if (!protocol.Equals("http") && !protocol.Equals("https"))
            {
                throw new InvalidOperationException("Only http & https protocols supported");
            }

            // set up the request for execution
            if (req.Method() == NSoup.Method.Get && req.Data().Count > 0)
            {
                SerialiseRequestUrl(req); // appends query string
            }

            HttpWebRequest conn = CreateConnection(req);

            if (req.Method() == NSoup.Method.Post)
            {
                conn.ContentType = "application/x-www-form-urlencoded";
                WritePost(req.Data(), conn.GetRequestStream());
            }

            HttpWebResponse response = (HttpWebResponse)conn.GetResponse();

            HttpStatusCode status = response.StatusCode;
            bool needsRedirect = false;
            if (status != HttpStatusCode.OK)
            {
                if (status == HttpStatusCode.Moved || status == HttpStatusCode.MovedPermanently || status == HttpStatusCode.SeeOther)
                {
                    needsRedirect = true;
                }
                else if (!req.IgnoreHttpErrors())
                {
                    throw new IOException(status + " error loading URL " + req.Url().ToString());
                }
            }

            Response res = new Response(previousResponse);
            res.SetupFromConnection(response, previousResponse);

            if (needsRedirect && req.FollowRedirects())
            {
                req.Url(new Uri(req.Url(), res.Header("Location")));

                foreach (KeyValuePair<string, string> cookie in res.Cookies()) // add response cookies to request (for e.g. login posts)
                {
                    req.Cookie(cookie.Key, cookie.Value);
                }

                return Execute(req, res);
            }

            res.req = req;

            //dataStream = conn.getErrorStream() != null ? conn.getErrorStream() : conn.getInputStream();
            //bodyStream = res.hasHeader("Content-Encoding") && res.header("Content-Encoding").equalsIgnoreCase("gzip") ?
            //        new BufferedInputStream(new GZIPInputStream(dataStream)) :
            //        new BufferedInputStream(dataStream);

            using (Stream inStream =
                (res.HasHeader("Content-Encoding") && res.Header("Content-Encoding").Equals("gzip")) ?
                    new GZipStream(response.GetResponseStream(), CompressionMode.Decompress) :
                    response.GetResponseStream())
            {
                res._byteData = DataUtil.ReadToByteBuffer(inStream);
                res._charset = DataUtil.GetCharsetFromContentType(res.ContentType()); // may be null, readInputStream deals with it
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

            if (!req.IgnoreContentType() && (_contentType == null || !(_contentType.StartsWith("text/") || _contentType.StartsWith("application/xml") || _contentType.StartsWith("application/xhtml+xml"))))
            {
                throw new IOException(string.Format("Unhandled content type \"{0}\" on URL {1}. Must be text/*, application/xml, or application/xhtml+xml",
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

        // set up connection defaults, and details from request
        private static HttpWebRequest CreateConnection(IRequest req)
        {
            HttpWebRequest conn = (HttpWebRequest)HttpWebRequest.Create(req.Url());

            conn.Method = req.Method().ToString();
            conn.AllowAutoRedirect = false; // don't rely on native redirection support
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

            return conn;
        }

        

        // set up url, method, header, cookies
        private void SetupFromConnection(HttpWebResponse conn, IResponse previousResponse)
        {

            _method = (Method)Enum.Parse(typeof(Method), conn.Method, true);

            _url = conn.ResponseUri;
            _statusCode = conn.StatusCode;
            _statusMessage = conn.StatusDescription;
            _contentType = conn.ContentType;

            // headers into map
            WebHeaderCollection resHeaders = conn.Headers;

            ProcessResponseHeaders(resHeaders);

            // if from a redirect, map previous response cookies into this response
            if (previousResponse != null)
            {
                foreach (KeyValuePair<string, string> prevCookie in previousResponse.Cookies())
                {
                    if (!HasCookie(prevCookie.Key))
                    {
                        Cookie(prevCookie.Key, prevCookie.Value);
                    }
                }
            }
        }

        public void ProcessResponseHeaders(WebHeaderCollection resHeaders)
        {
            foreach (string name in resHeaders.Keys)
            {
                if (name == null)
                {
                    continue; // http/1.1 line
                }

                string[] values = resHeaders[name].Split(';');

                if (name.Equals("Set-Cookie", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (string value in values)
                    {
                        if (value == null)
                        {
                            continue;
                        }

                        TokenQueue cd = new TokenQueue(value);
                        string cookieName = cd.ChompTo("=").Trim();
                        string cookieVal = cd.ConsumeTo(";").Trim();

                        if (cookieVal == null)
                        {
                            cookieVal = string.Empty;
                        }

                        // ignores path, date, domain, secure et al. req'd?
                        if (StringUtil.In(cookieName.ToLowerInvariant(), "domain", "path", "expires", "max-age", "secure", "httponly"))
                        {
                            // This is added for NSoup, since we do headers a bit differently around here.
                            continue;
                        }

                        // name not blank, value not null
                        if (!string.IsNullOrEmpty(cookieName))
                        {
                            Cookie(cookieName, cookieVal);
                        }
                    }
                }
                else
                { // only take the first instance of each header
                    if (values.Length > 1)
                    {
                        Header(name, values[0]);
                    }
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

        // for get url reqs, serialise the data map into the url
        private static void SerialiseRequestUrl(IRequest req)
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

            req.Url(new Uri(url.ToString()));
            req.Data().Clear(); // moved into url as get params
        }

        public int NumRedirects
        {
            get
            {
                return _numRedirects;
            }
        }
    }

    public class Request : ConnectionBase<IRequest>, IRequest
    {
        private int _timeoutMilliseconds;
        private bool _followRedirects;
        private ICollection<KeyVal> _data;
        private bool _ignoreHttpErrors = false;
        private bool _ignoreContentType = false;

        public Request()
        {
            _timeoutMilliseconds = 3000;
            _followRedirects = true;
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

            _timeoutMilliseconds = millis;

            return this;
        }

        public bool FollowRedirects()
        {
            return _followRedirects;
        }

        public IRequest FollowRedirects(bool followRedirects)
        {
            this._followRedirects = followRedirects;
            
            return this;
        }

        public bool IgnoreHttpErrors()
        {
            return _ignoreHttpErrors;
        }

        public void IgnoreHttpErrors(bool ignoreHttpErrors)
        {
            this._ignoreHttpErrors = ignoreHttpErrors;
        }

        public bool IgnoreContentType()
        {
            return _ignoreContentType;
        }

        public void IgnoreContentType(bool ignoreContentType)
        {
            this._ignoreContentType = ignoreContentType;
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