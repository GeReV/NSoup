using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup;

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

            Document normaliseTitle = NSoupClient.Parse("<title>   Hello\nthere   \n   now   \n");
            Assert.AreEqual("Hello there now", normaliseTitle.Title);
        }

        [TestMethod]
        public void testOutputEncoding()
        {
            Document doc = NSoup.NSoupClient.Parse("<p title=π>π & < > </p>");
            // default is utf-8
            Assert.AreEqual("<p title=\"π\">π &amp; &lt; &gt; </p>", doc.Body.Html());
            Assert.AreEqual("UTF-8", doc.OutputSettings().Encoding.WebName.ToUpperInvariant());

            doc.OutputSettings().SetEncoding("ascii");
            Assert.AreEqual(Entities.EscapeMode.Base, doc.OutputSettings().EscapeMode);
            Assert.AreEqual("<p title=\"&#960;\">&#960; &amp; &lt; &gt; </p>", doc.Body.Html());

            doc.OutputSettings().EscapeMode = Entities.EscapeMode.Extended;
            Assert.AreEqual("<p title=\"&pi;\">&pi; &amp; &lt; &gt; </p>", doc.Body.Html());
        }

        [TestMethod]
        public void testXhtmlReferences()
        {
            Document doc = NSoupClient.Parse("&lt; &gt; &amp; &quot; &apos; &times;");
            doc.OutputSettings().EscapeMode = Entities.EscapeMode.Xhtml;
            Assert.AreEqual("&lt; &gt; &amp; &quot; &apos; ×", doc.Body.Html());
        }

        [TestMethod]
        public void testNormalisesStructure()
        {
            Document doc = NSoupClient.Parse("<html><head><script>one</script><noscript><p>two</p></noscript></head><body><p>three</p></body><p>four</p></html>");
            Assert.AreEqual("<html><head><script>one</script><noscript></noscript></head><body><p>two</p><p>three</p><p>four</p></body></html>", TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void testClone()
        {
            Document doc = NSoupClient.Parse("<title>Hello</title> <p>One<p>Two");
            Document clone = (Document)doc.Clone();

            Assert.AreEqual("<html><head><title>Hello</title> </head><body><p>One</p><p>Two</p></body></html>", TextUtil.StripNewLines(clone.Html()));
            clone.Title = "Hello there";
            clone.Select("p").First.Text("One more").Attr("id", "1");
            Assert.AreEqual("<html><head><title>Hello there</title> </head><body><p id=\"1\">One more</p><p>Two</p></body></html>", TextUtil.StripNewLines(clone.Html()));
            Assert.AreEqual("<html><head><title>Hello</title> </head><body><p>One</p><p>Two</p></body></html>", TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void testClonesDeclarations()
        {
            Document doc = NSoupClient.Parse("<!DOCTYPE html><html><head><title>Doctype test");
            Document clone = (Document)doc.Clone();

            Assert.AreEqual(doc.Html(), clone.Html());
            Assert.AreEqual("<!DOCTYPE html><html><head><title>Doctype test</title></head><body></body></html>",
                    TextUtil.StripNewLines(clone.Html()));
        }
    }
}
