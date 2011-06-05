using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Select;

namespace Test.Nodes
{
    /// <summary>
    /// Test suite for attribute parser.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class AttributeParseTest
    {
        public AttributeParseTest()
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

        [TestMethod]
        public void parsesRoughAttributeString()
        {
            string html = "<a id=\"123\" class=\"baz = 'bar'\" style = 'border: 2px'qux zim foo = 12 mux=18 />";
            // should be: <id=123>, <class=baz = 'bar'>, <qux=>, <zim=>, <foo=12>, <mux.=18>

            Element el = NSoup.NSoupClient.Parse(html).GetElementsByTag("a")[0];
            Attributes attr = el.Attributes;
            Assert.AreEqual(7, attr.Count);
            Assert.AreEqual("123", attr["id"]);
            Assert.AreEqual("baz = 'bar'", attr["class"]);
            Assert.AreEqual("border: 2px", attr["style"]);
            Assert.AreEqual("", attr["qux"]);
            Assert.AreEqual("", attr["zim"]);
            Assert.AreEqual("12", attr["foo"]);
            Assert.AreEqual("18", attr["mux"]);
        }

        [TestMethod]
        public void parsesEmptyString()
        {
            string html = "<a />";
            Element el = NSoup.NSoupClient.Parse(html).GetElementsByTag("a")[0];
            Attributes attr = el.Attributes;
            Assert.AreEqual(0, attr.Count);
        }

        [TestMethod]
        public void emptyOnNoKey()
        {
            string html = "<a =empty />";
            Element el = NSoup.NSoupClient.Parse(html).GetElementsByTag("a")[0];
            Attributes attr = el.Attributes;
            Assert.AreEqual(0, attr.Count);
        }

        [TestMethod]
        public void strictAttributeUnescapes()
        {
            string html = "<a id=1 href='?foo=bar&mid&lt=true'>One</a> <a id=2 href='?foo=bar&lt;qux&lg=1'>Two</a>";
            Elements els = NSoup.NSoupClient.Parse(html).Select("a");
            Assert.AreEqual("?foo=bar&mid&lt=true", els.First.Attr("href"));
            Assert.AreEqual("?foo=bar<qux&lg=1", els.Last.Attr("href"));
        }
    }
}
