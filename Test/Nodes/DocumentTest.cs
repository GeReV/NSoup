using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;

namespace Test.Nodes
{
    /// <summary>
    /// Tests for Document.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class DocumentTest
    {
        public DocumentTest()
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
        public void setTextPreservesDocumentStructure()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>Hello</p>");
            doc.Text("Replaced");
            Assert.AreEqual("Replaced", doc.Text());
            Assert.AreEqual("Replaced", doc.Body.Text());
            Assert.AreEqual(1, doc.Select("head").Count);
        }

        [TestMethod]
        public void testTitles()
        {
            Document noTitle = NSoup.NSoupClient.Parse("<p>Hello</p>");
            Document withTitle = NSoup.NSoupClient.Parse("<title>First</title><title>Ignore</title><p>Hello</p>");

            Assert.AreEqual("", noTitle.Title);
            noTitle.Title = "Hello";
            Assert.AreEqual("Hello", noTitle.Title);
            Assert.AreEqual("Hello", noTitle.Select("title").First.Text());

            Assert.AreEqual("First", withTitle.Title);
            withTitle.Title = "Hello";
            Assert.AreEqual("Hello", withTitle.Title);
            Assert.AreEqual("Hello", withTitle.Select("title").First.Text());
        }

        [TestMethod]
        public void testOutputEncoding()
        {
            Document doc = NSoup.NSoupClient.Parse("<p title=π>π & < > </p>");
            // default is utf-8
            Assert.AreEqual("<p title=\"π\">π &amp; &lt; &gt; </p>", doc.Body.Html());
            Assert.AreEqual("UTF-8", doc.Settings.Encoding.WebName.ToUpperInvariant());

            doc.Settings.SetEncoding("ascii");
            Assert.AreEqual(Entities.EscapeMode.Base, doc.Settings.EscapeMode);
            Assert.AreEqual("<p title=\"&#960;\">&#960; &amp; &lt; &gt; </p>", doc.Body.Html());

            doc.Settings.EscapeMode = Entities.EscapeMode.Extended;
            Assert.AreEqual("<p title=\"&pi;\">&pi; &amp; &lt; &gt; </p>", doc.Body.Html());
        }
    }
}
