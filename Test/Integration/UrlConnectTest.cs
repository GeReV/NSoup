using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;

namespace Test.Integration
{
    /// <summary>
    /// Tests the URL connection. Not enabled by default, so tests don't require network connection.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class UrlConnectTest
    {
        public UrlConnectTest()
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

        //[TestMethod] // uncomment to enable test
        public void fetchURl()
        {
            string url = "http://www.google.com"; // no trailing / to force redir
            Document doc = NSoup.NSoupClient.Parse(new Uri(url), 10 * 1000);
            Assert.IsTrue(doc.Title.Contains("Google"));
        }

        //[TestMethod] // uncomment to enble
        public void fetchBaidu()
        {
            Document doc = NSoup.NSoupClient.Parse(new Uri("http://www.baidu.com/"), 10 * 1000);
            Assert.AreEqual("GB2312", doc.Settings.Encoding.WebName.ToUpperInvariant());
        }

        //[TestMethod] // uncomment to enable
        public void exceptOnUnknownContentType()
        {
            string url = "http://jsoup.org/rez/osi_logo.png"; // not text/* but image/png, should throw
            bool threw = false;
            try
            {
                Document doc = NSoup.NSoupClient.Parse(new Uri(url), 3000);
            }
            catch (Exception)
            {
                threw = true;
            }
            Assert.IsTrue(threw);
        }

        public void noop() { }
    }
}
