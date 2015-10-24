using NSoup.Nodes;
using NSoup.Parse;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Web;

namespace NSoup.Helper
{
	public class HttpConnection : IConnection
	{
		#region IConnection Members

		public static readonly string CONTENT_ENCODING = "Content-Encoding";

		public static IConnection Connect(string url)
		{
			var con = new HttpConnection();
			con.Url(url);
			return con;
		}

		public static IConnection Connect(Uri url)
		{
			var con = new HttpConnection();
			con.Url(url);
			return con;
		}

		private static string EncodeUrl(string url)
		{
			if(string.IsNullOrWhiteSpace(url))
			{
				return null;
			}

			return url.Replace(" ", "%20");
		}

		private static string EncodeMimeName(string val)
		{
			if (string.IsNullOrWhiteSpace(val))
			{
				return null;
			}

			return val.Replace("\"", "%22");
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

		public IConnection MaxBodySize(int bytes)
		{
			req.MaxBodySize(bytes);
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

		public IConnection ValidateTLSCertificates(bool value)
		{
			req.ValidateTLSCertificates(value);
			return this;
		}

		public IConnection Data(string key, string value)
		{
			req.Data(KeyVal.Create(key, value));
			return this;
		}

		public IConnection Data(string key, string fileName, Stream stream)
		{
			req.Data(KeyVal.Create(key, fileName, stream));
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
				var key = keyvals[i];
				var value = keyvals[i + 1];

				if (string.IsNullOrWhiteSpace(key))
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

		public IConnection Cookies(IDictionary<string, string> cookies)
		{
			if (cookies == null)
			{
				throw new ArgumentNullException("cookies");
			}
			foreach (var entry in cookies)
			{
				req.Cookie(entry.Key, entry.Value);
			}

			return this;
		}

		public IConnection Parser(Parser parser)
		{
			req.Parser(parser);
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
			res = Helper.Response.Execute(req);
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
		protected Uri url;
		protected Method method;
		protected IDictionary<string, string> headers;
		protected IDictionary<string, string> cookies;

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
			headers = new SortedDictionary<string, string>(new CaseInsensitiveComparer());
			cookies = new SortedDictionary<string, string>();
		}

		public Uri Url()
		{
			return url;
		}

		public IConnectionBase<T> Url(Uri url)
		{
			if (url == null)
			{
				throw new ArgumentNullException("url");
			}

			this.url = url;
			return this;
		}

		public Method Method()
		{
			return method;
		}

		public IConnectionBase<T> Method(Method method)
		{
			this.method = method;
			return this;
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

			headers[name] = value;

			return this;
		}

		public bool HasHeader(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Header name must not be empty", "name");
			}

			return GetHeaderCaseInsensitive(name) != null;
		}

		public bool HasHeaderWithValue(String name, String value)
		{
			return HasHeader(name) && Header(name).Equals(value, StringComparison.InvariantCultureIgnoreCase);
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
				headers.Remove(entry.Value.Key); // ensures correct case
			}

			return this;
		}

		public IDictionary<string, string> Headers()
		{
			return headers;
		}

		private string GetHeaderCaseInsensitive(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name", "Header name must not be null");
			}

			// quick evals for common case of title case, lower case, then scan for mixed
			string value = null;

			if (!headers.TryGetValue(name, out value)) // Also case insensitive thanks to the CaseInsensitiveComparer.
			{
				KeyValuePair<string, string>? entry = ScanHeaders(name);
				if (entry != null)
				{
					value = entry.Value.Value;
				}
			}

			return value;
		}

		private KeyValuePair<string, string>? ScanHeaders(string name)
		{
			var lc = name.ToLowerInvariant();
			foreach (var entry in headers)
			{
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

			return cookies[name];
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

			cookies[name] = value;

			return this;
		}

		public bool HasCookie(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Cookie name must not be empty", "name");
			}

			return cookies.ContainsKey(name);
		}

		public IConnectionBase<T> RemoveCookie(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Cookie name must not be empty", "name");
			}

			cookies.Remove(name);

			return this;
		}

		public IDictionary<string, string> Cookies()
		{
			return cookies;
		}
	}

	public class Response : ConnectionBase<IResponse>, IResponse
	{
		private static readonly int MAX_REDIRECTS = 20;
		private HttpStatusCode statusCode;
		private string statusMessage;
		private byte[] byteData;
		private string charset;
		private string contentType;
		private bool executed = false;
		private int numRedirects = 0;
		private IRequest req;

		public Response()
			: base()
		{ }

		private Response(IResponse previousResponse)
			: base()
		{
			if (previousResponse != null)
			{
				numRedirects = previousResponse.NumRedirects + 1;
				if (numRedirects >= MAX_REDIRECTS)
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
				// throw new MalformedURLException
				throw new InvalidOperationException("Only http & https protocols supported");
			}

			// set up the request for execution
			if (req.Method() == NSoup.Method.Get && req.Data().Count > 0)
			{
				SerialiseRequestUrl(req); // appends query string
			}

			var conn = CreateConnection(req);
			HttpWebResponse response = null;

			try
			{
				if (req.Method() == NSoup.Method.Post)
				{
					conn.ContentType = "application/x-www-form-urlencoded";
					WritePost(req.Data(), conn.GetRequestStream());
				}

				response = (HttpWebResponse)conn.GetResponse();
			}
			catch (WebException e)
			{
				response = e.Response as HttpWebResponse;

				if (response != null)
				{
					return ProcessResponse(response, req, previousResponse);
				}
			}

			return ProcessResponse(response, req, previousResponse);
		}

		private static Response ProcessResponse(HttpWebResponse response, IRequest req, IResponse previousResponse)
		{
			var needsRedirect = false;
			var status = response.StatusCode;

			if (status != HttpStatusCode.OK)
			{
				if (status == HttpStatusCode.Found || status == HttpStatusCode.MovedPermanently || status == HttpStatusCode.SeeOther)
				{
					// In .NET (4.0+ ?), Moved and MovedPermanently have the same value.
					needsRedirect = true;
				}
				else if (!req.IgnoreHttpErrors())
				{
					throw new HttpStatusException("HTTP error fetching URL", (int)status, req.Url().ToString());
				}
			}

			var res = new Response(previousResponse);
			res.SetupFromConnection(response, previousResponse);

			if (needsRedirect && req.FollowRedirects())
			{
				req.Method(NSoup.Method.Get);
				req.Data().Clear();
				req.Url(new Uri(req.Url(), res.Header("Location")));

				foreach (var cookie in res.Cookies()) // add response cookies to request (for e.g. login posts)
				{
					req.Cookie(cookie.Key, cookie.Value);
				}

				return Execute(req, res);
			}

			res.req = req;

			// check that we can handle the returned content type; if not, abort before fetching it
			var contentType = res.ContentType();

			if (contentType != null && !req.IgnoreContentType() && (!(contentType.StartsWith("text/") || contentType.StartsWith("application/xml") || contentType.StartsWith("application/xhtml+xml"))))
			{
				throw new UnsupportedMimeTypeException("Unhandled content type. Must be text/*, application/xml, or application/xhtml+xml",
						contentType, req.Url().ToString());
			}

			//dataStream = conn.getErrorStream() != null ? conn.getErrorStream() : conn.getInputStream();
			//bodyStream = res.hasHeader("Content-Encoding") && res.header("Content-Encoding").equalsIgnoreCase("gzip") ?
			//        new BufferedInputStream(new GZIPInputStream(dataStream)) :
			//        new BufferedInputStream(dataStream);

			using (var inStream =
				(res.HasHeader("Content-Encoding") && res.Header("Content-Encoding").Equals("gzip")) ?
					new GZipStream(response.GetResponseStream(), CompressionMode.Decompress) :
					response.GetResponseStream())
			{
				res.byteData = DataUtil.ReadToByteBuffer(inStream);
				res.charset = DataUtil.GetCharsetFromContentType(res.ContentType()); // may be null, readInputStream deals with it
			}

			res.executed = true;

			return res;
		}

		public HttpStatusCode StatusCode()
		{
			return statusCode;
		}

		public string StatusMessage()
		{
			return statusMessage;
		}

		public string Charset()
		{
			return charset;
		}

		public string ContentType()
		{
			return contentType;
		}

		public Document Parse()
		{
			if (!executed)
			{
				throw new InvalidOperationException("Request must be executed (with .Execute(), .Get(), or .Post() before parsing response ");
			}

			var doc = DataUtil.ParseByteData(byteData, charset, url.ToString(), req.Parser());

			charset = doc.OutputSettings().Encoding.WebName.ToUpperInvariant(); // update charset from meta-equiv, possibly
			return doc;
		}

		public string Body()
		{
			if (!executed)
			{
				throw new InvalidOperationException("Request must be executed (with .Execute(), .Get(), or .Post() before getting response body");
			}

			// charset gets set from header on execute, and from meta-equiv on parse. parse may not have happened yet
			return string.IsNullOrWhiteSpace(charset) ? DataUtil.DefaultEncoding.GetString(byteData) :
				Encoding.GetEncoding(charset).GetString(byteData);
		}

		public byte[] BodyAsBytes()
		{
			if (!executed)
			{
				throw new InvalidOperationException("Request must be executed (with .Execute(), .Get(), or .Post() before getting response body");
			}
			return byteData;
		}

		// set up connection defaults, and details from request
		private static HttpWebRequest CreateConnection(IRequest req)
		{
			var conn = (HttpWebRequest)HttpWebRequest.Create(req.Url());
			conn.Method = req.Method().ToString();
			conn.AllowAutoRedirect = false; // don't rely on native redirection support
			conn.Timeout = req.Timeout();
			conn.ReadWriteTimeout = req.Timeout();

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

			method = (Method)Enum.Parse(typeof(Method), conn.Method, true);

			url = conn.ResponseUri;
			statusCode = conn.StatusCode;
			statusMessage = conn.StatusDescription;
			contentType = conn.ContentType;

			// headers into map
			var resHeaders = conn.Headers;

			ProcessResponseHeaders(resHeaders);

			// if from a redirect, map previous response cookies into this response
			if (previousResponse != null)
			{
				foreach (var prevCookie in previousResponse.Cookies())
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
				if (string.IsNullOrWhiteSpace(name))
				{
					continue; // http/1.1 line
				}

				var value = resHeaders[name]; //.Split(';');

				if (name.Equals("Set-Cookie", StringComparison.InvariantCultureIgnoreCase))
				{
					var values = resHeaders["Set-Cookie"].Split(';', ',');
					foreach (string v in values)
					{
						if (string.IsNullOrWhiteSpace(v))
						{
							continue;
						}

						var cd = new TokenQueue(v);
						var cookieName = cd.ChompTo("=").Trim();
						var cookieVal = cd.ConsumeTo(";").Trim();

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
				{
					if (!string.IsNullOrEmpty(value))
					{
						Header(name, /*values[0]*/ value);
					}
				}
			}
		}

		private static void WritePost(ICollection<KeyVal> data, Stream outputStream)
		{
			var sb = new StringBuilder();

			var first = true;
			foreach (var keyVal in data)
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

			var bytes = DataUtil.DefaultEncoding.GetBytes(sb.ToString());

			outputStream.Write(bytes, 0, bytes.Length);
			outputStream.Close();
		}

		private static string GetRequestCookieString(IRequest req)
		{
			var sb = new StringBuilder();
			var first = true;
			foreach (var cookie in req.Cookies())
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
			var input = req.Url();
			var url = new StringBuilder();
			var first = true;
			// reconstitute the query, ready for appends
			url.Append(input.Scheme).Append("://").Append(input.Authority)
				.Append(input.AbsolutePath).Append("?");

			if (!string.IsNullOrEmpty(input.Query))
			{
				url.Append(input.Query);
				first = false;
			}

			foreach (var keyVal in req.Data())
			{
				if (!first)
				{
					url.Append('&');
				}
				else
				{
					first = false;
				}

				url.Append(HttpUtility.UrlEncode(keyVal.Key(), DataUtil.DefaultEncoding))
					.Append('=').Append(HttpUtility.UrlEncode(keyVal.Value(), DataUtil.DefaultEncoding));
			}

			req.Url(new Uri(url.ToString()));
			req.Data().Clear(); // moved into url as get params
		}

		public int NumRedirects
		{
			get
			{
				return numRedirects;
			}
		}
	}

	public class Request : ConnectionBase<IRequest>, IRequest
	{
		private int timeoutMilliseconds;
		private int maxBodySizeBytes;
		private bool followRedirects;
		private ICollection<KeyVal> data;
		private bool ignoreHttpErrors = false;
		private bool ignoreContentType = false;
		private Parser parser;
		private bool parserDefined = false; // called parser(...) vs initialized in ctor
		private bool validateTSLCertificates = true;
		private string postDataCharset = DataUtil.DefaultEncoding.ToString();

		public Request()
		{
			timeoutMilliseconds = 3000;
			maxBodySizeBytes = 1024 * 1024;
			followRedirects = true;
			data = new List<KeyVal>();
			method = NSoup.Method.Get;
			headers["Accept-Encoding"] = "gzip";
			parser = Parse.Parser.HtmlParser();
		}

		public int Timeout()
		{
			return timeoutMilliseconds;
		}

		public IRequest Timeout(int millis)
		{
			if (millis < 0)
			{
				throw new ArgumentOutOfRangeException("Timeout milliseconds must be 0 (infinite) or greater");
			}

			timeoutMilliseconds = millis;

			return this;
		}

		public int MaxBodySize()
		{
			return maxBodySizeBytes;
		}

		public IRequest MaxBodySize(int bytes)
		{
			if (bytes < 0)
			{
				throw new ArgumentOutOfRangeException("Max Size must be 0 (infinite) or greater");
			}
			
			maxBodySizeBytes = bytes;
			return this;
		}

		public bool FollowRedirects()
		{
			return followRedirects;
		}

		public IRequest FollowRedirects(bool followRedirects)
		{
			this.followRedirects = followRedirects;
			return this;
		}

		public bool IgnoreHttpErrors()
		{
			return ignoreHttpErrors;
		}

		public IRequest IgnoreHttpErrors(bool ignoreHttpErrors)
		{
			this.ignoreHttpErrors = ignoreHttpErrors;
			return this;
		}

		public bool ValidateTLSCertificates()
		{
			return validateTSLCertificates;
		}

		public void ValidateTLSCertificates(bool value)
		{
			validateTSLCertificates = value;
		}

		public bool IgnoreContentType()
		{
			return ignoreContentType;
		}

		public IRequest IgnoreContentType(bool ignoreContentType)
		{
			this.ignoreContentType = ignoreContentType;
			return this;
		}

		public IRequest Data(KeyVal keyval)
		{
			if (keyval == null)
			{
				throw new ArgumentNullException("keyval");
			}

			data.Add(keyval);

			return this;
		}

		public ICollection<KeyVal> Data()
		{
			return data;
		}

		public IRequest Parser(Parser parser)
		{
			this.parser = parser;
			return this;
		}

		public Parser Parser()
		{
			return parser;
		}
	}

	public class KeyVal : IKeyVal
	{
		private string key;
		private string value;
		private Stream stream;

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

		public static KeyVal Create(string key, string value, Stream stream)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentException("Data key must not be empty", "key");
			}

			if (value == null)
			{
				throw new ArgumentNullException("value", "Data value must not be null");
			}

			return new KeyVal(key, value, stream);
		}

		private KeyVal(string key, string value)
		{
			this.key = key;
			this.value = value;
		}

		private KeyVal(string key, string value, Stream stream)
		{
			this.key = key;
			this.value = value;
			this.stream = stream;
		}

		#region IKeyVal Members

		public IKeyVal Key(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				throw new ArgumentException("Data key must not be empty", "key");
			}

			this.key = key;

			return this;
		}

		public string Key()
		{
			return key;
		}

		public IKeyVal Value(string value)
		{
			if (value == null)
			{
				throw new ArgumentNullException("value", "Data value must not be null");
			}

			this.value = value;

			return this;
		}

		public string Value()
		{
			return value;
		}

		public IKeyVal InputStream(Stream inputStream)
		{
			if (inputStream == null)
			{
				throw new ArgumentNullException("inputStream", "Data input stream must not be null");
			}

			this.stream = inputStream;
			return this;
		}

		public Stream InputStream()
		{
			return stream;
		}

		public bool HasInputStream()
		{
			return stream != null;
		}

		#endregion

		public override string ToString()
		{
			return string.Concat(key, "=", value);
		}
	}
}