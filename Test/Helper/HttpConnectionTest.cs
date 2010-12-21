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
    }
}
