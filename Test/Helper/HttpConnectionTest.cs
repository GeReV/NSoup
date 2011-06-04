using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup;
using NSoup.Helper;

namespace Test.Nodes
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
            IConnection con = HttpConnection.Connect("http://example.com");
            con.Response().Parse();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void throwsExceptionOnBodyWithoutExecute()
        {
            IConnection con = HttpConnection.Connect("http://example.com");
            con.Response().Body();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void throwsExceptionOnBodyAsBytesWithoutExecute()
        {
            IConnection con = HttpConnection.Connect("http://example.com");
            con.Response().BodyAsBytes();
        }

        [TestMethod]
        public void caseInsensitiveHeaders()
        {
            IResponse res = new Response();
            IDictionary<string, string> headers = res.Headers();
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
        public void connectWithUrl()
        {
            IConnection con = HttpConnection.Connect(new Uri("http://example.com"));
            Assert.AreEqual("http://example.com/", con.Request().Url().ToString());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void throwsOnMalformedUrl()
        {
            IConnection con = HttpConnection.Connect("bzzt");
        }

        [TestMethod]
        public void userAgent()
        {
            IConnection con = HttpConnection.Connect("http://example.com/");
            con.UserAgent("Mozilla");
            Assert.AreEqual("Mozilla", con.Request().Header("User-Agent"));
        }

        [TestMethod]
        public void timeout()
        {
            IConnection con = HttpConnection.Connect("http://example.com/");
            con.Timeout(1000);
            Assert.AreEqual(1000, con.Request().Timeout());
        }

        [TestMethod]
        public void referrer()
        {
            IConnection con = HttpConnection.Connect("http://example.com/");
            con.Referrer("http://foo.com");
            Assert.AreEqual("http://foo.com", con.Request().Header("Referer"));
        }

        [TestMethod]
        public void method()
        {
            IConnection con = HttpConnection.Connect("http://example.com/");
            Assert.AreEqual(Method.Get, con.Request().Method());
            con.Method(Method.Post);
            Assert.AreEqual(Method.Post, con.Request().Method());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void throwsOnOdddData()
        {
            IConnection con = HttpConnection.Connect("http://example.com/");
            con.Data("Name", "val", "what");
        }

        [TestMethod]
        public void data()
        {
            IConnection con = HttpConnection.Connect("http://example.com/");
            con.Data("Name", "Val", "Foo", "bar");
            ICollection<KeyVal> values = con.Request().Data();
            Object[] data = values.ToArray();
            KeyVal one = (KeyVal)data[0];
            KeyVal two = (KeyVal)data[1];
            Assert.AreEqual("Name", one.Key());
            Assert.AreEqual("Val", one.Value());
            Assert.AreEqual("Foo", two.Key());
            Assert.AreEqual("bar", two.Value());
        }

        [TestMethod]
        public void cookie()
        {
            IConnection con = HttpConnection.Connect("http://example.com/");
            con.Cookie("Name", "Val");
            Assert.AreEqual("Val", con.Request().Cookie("Name"));
        }
    }
}
