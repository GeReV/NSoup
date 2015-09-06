using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup;
using NSoup.Helper;
using System.IO;

namespace Test.Helper
{

	[TestClass]
	public class HttpConnectionTest
	{
		public HttpConnectionTest()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
		{
			get
			{
				return testContextInstance;
			}
			set
			{
				testContextInstance = value;
			}
		}

		#region Additional test attributes
		//
		// You can use the following additional attributes as you write your tests:
		//
		// Use ClassInitialize to run code before running the first test in the class
		// [ClassInitialize()]
		// public static void MyClassInitialize(TestContext testContext) { }
		//
		// Use ClassCleanup to run code after all tests in a class have run
		// [ClassCleanup()]
		// public static void MyClassCleanup() { }
		//
		// Use TestInitialize to run code before running each test 
		// [TestInitialize()]
		// public void MyTestInitialize() { }
		//
		// Use TestCleanup to run code after each test has run
		// [TestCleanup()]
		// public void MyTestCleanup() { }
		//
		#endregion

		/* most actual network http connection tests are in integration */

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void throwsExceptionOnParseWithoutExecute()
		{
			var con = HttpConnection.Connect("http://example.com");
			con.Response().Parse();
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void throwsExceptionOnBodyWithoutExecute()
		{
			var con = HttpConnection.Connect("http://example.com");
			con.Response().Body();
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void throwsExceptionOnBodyAsBytesWithoutExecute()
		{
			var con = HttpConnection.Connect("http://example.com");
			con.Response().BodyAsBytes();
		}

		[TestMethod]
		public void caseInsensitiveHeaders()
		{
			var res = new Response();
			var headers = res.Headers();
			headers["Accept-Encoding"] = "gzip";
			headers["content-type"] = "text/html";
			headers["refErrer"] = "http://example.com";

			Assert.IsTrue(res.HasHeader("Accept-Encoding"));
			Assert.IsTrue(res.HasHeader("accept-encoding"));
			Assert.IsTrue(res.HasHeader("accept-Encoding"));

			Assert.AreEqual("gzip", res.Header("accept-Encoding"));
			Assert.AreEqual("text/html", res.Header("Content-Type"));
			Assert.AreEqual("http://example.com", res.Header("Referrer"));

			res.RemoveHeader("Content-Type");
			Assert.IsFalse(res.HasHeader("content-type"));

			res.Header("accept-encoding", "deflate");
			Assert.AreEqual("deflate", res.Header("Accept-Encoding"));
			Assert.AreEqual("deflate", res.Header("accept-Encoding"));
		}

		[TestMethod]
		public void sameHeadersCombineWithComma()
		{
			var headers = new System.Net.WebHeaderCollection();
			var cacheValues = new List<string>();
			cacheValues.Add("no-cache");
			cacheValues.Add("no-store");
			headers.Set("Cache-Control", cacheValues.Join(", "));
			var res = new Response();
			res.ProcessResponseHeaders(headers);
			Assert.AreEqual("no-cache, no-store", res.Header("Cache-Control"));
		}

		[TestMethod]
		public void ignoresEmptySetCookies()
		{
			// prep http response header map
			var headers = new System.Net.WebHeaderCollection();
			headers["Set-Cookie"] = string.Empty;
			Response res = new Response();
			res.ProcessResponseHeaders(headers);
			Assert.AreEqual(0, res.Cookies().Count);
		}

		[TestMethod]
		public void ignoresEmptyCookieNameAndVals()
		{
			// prep http response header map
			var headers = new System.Net.WebHeaderCollection();
			var cookieStrings = new List<string>();
			cookieStrings.Add(null);
			cookieStrings.Add("");
			cookieStrings.Add("one");
			cookieStrings.Add("two=");
			cookieStrings.Add("three=");
			cookieStrings.Add("four=data; Domain=.example.com; Path=/");
			headers.Set("Set-Cookie", cookieStrings.Join(";"));

			var res = new Response();
			res.ProcessResponseHeaders(headers);
			Assert.AreEqual(4, res.Cookies().Count);
			Assert.AreEqual(string.Empty, res.Cookie("one"));
			Assert.AreEqual(string.Empty, res.Cookie("two"));
			Assert.AreEqual(string.Empty, res.Cookie("three"));
			Assert.AreEqual("data", res.Cookie("four"));
		}

		[TestMethod]
		public void connectWithUrl()
		{
			var con = HttpConnection.Connect(new Uri("http://example.com"));
			Assert.AreEqual("http://example.com/", con.Request().Url().ToString());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void throwsOnMalformedUrl()
		{
			var con = HttpConnection.Connect("bzzt");
		}

		[TestMethod]
		public void userAgent()
		{
			var con = HttpConnection.Connect("http://example.com/");
			con.UserAgent("Mozilla");
			Assert.AreEqual("Mozilla", con.Request().Header("User-Agent"));
		}

		[TestMethod]
		public void timeout()
		{
			var con = HttpConnection.Connect("http://example.com/");
			con.Timeout(1000);
			Assert.AreEqual(1000, con.Request().Timeout());
		}

		[TestMethod]
		public void referrer()
		{
			var con = HttpConnection.Connect("http://example.com/");
			con.Referrer("http://foo.com");
			Assert.AreEqual("http://foo.com", con.Request().Header("Referer"));
		}

		[TestMethod]
		public void method()
		{
			var con = HttpConnection.Connect("http://example.com/");
			Assert.AreEqual(Method.Get, con.Request().Method());
			con.Method(Method.Post);
			Assert.AreEqual(Method.Post, con.Request().Method());
		}

		[TestMethod]
		[ExpectedException(typeof(InvalidOperationException))]
		public void throwsOnOdddData()
		{
			var con = HttpConnection.Connect("http://example.com/");
			con.Data("Name", "val", "what");
		}

		[TestMethod]
		public void data()
		{
			var con = HttpConnection.Connect("http://example.com/");
			con.Data("Name", "Val", "Foo", "bar");
			var values = con.Request().Data();
			var data = values.ToArray();
			var one = data[0];
			var two = data[1];
			Assert.AreEqual("Name", one.Key());
			Assert.AreEqual("Val", one.Value());
			Assert.AreEqual("Foo", two.Key());
			Assert.AreEqual("bar", two.Value());
		}

		[TestMethod]
		public void cookie()
		{
			var con = HttpConnection.Connect("http://example.com/");
			con.Cookie("Name", "Val");
			Assert.AreEqual("Val", con.Request().Cookie("Name"));
		}

		[TestMethod]
		public void InputStream()
		{
			var kv = KeyVal.Create("file", "thumb.jpg", new MemoryStream());
			Assert.AreEqual("file", kv.Key());
			Assert.AreEqual("thumb.jpg", kv.Value());
			Assert.IsTrue(kv.HasInputStream());

			kv = KeyVal.Create("one", "two");
			Assert.AreEqual("one", kv.Key());
			Assert.AreEqual("two", kv.Value());
			Assert.IsFalse(kv.HasInputStream());
		}
	}
}
