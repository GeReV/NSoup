using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Parse;

namespace Test.Nodes
{
    /// <summary>
    /// Tests Nodes
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class NodeTest
    {
        public NodeTest()
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
        public void handlesBaseUri()
        {
            Tag tag = Tag.ValueOf("a");
            Attributes attribs = new Attributes();
            attribs.Add("relHref", "/foo");
            attribs.Add("absHref", "http://bar/qux");

            Element noBase = new Element(tag, "", attribs);
            Assert.AreEqual("", noBase.AbsUrl("relHref")); // with no base, should NOT fallback to href attrib, whatever it is
            Assert.AreEqual("http://bar/qux", noBase.AbsUrl("absHref")); // no base but valid attrib, return attrib

            Element withBase = new Element(tag, "http://foo/", attribs);
            Assert.AreEqual("http://foo/foo", withBase.AbsUrl("relHref")); // construct abs from base + rel
            Assert.AreEqual("http://bar/qux", withBase.AbsUrl("absHref")); // href is abs, so returns that
            Assert.AreEqual("", withBase.AbsUrl("noval"));

            Element dodgyBase = new Element(tag, "wtf://no-such-protocol/", attribs);
            Assert.AreEqual("http://bar/qux", dodgyBase.AbsUrl("absHref")); // base fails, but href good, so get that
            Assert.AreEqual("", dodgyBase.AbsUrl("relHref")); // base fails, only rel href, so return nothing 
        }

        [TestMethod]
        public void handlesAbsPrefix()
        {
            Document doc = NSoup.NSoupClient.Parse("<a href=/foo>Hello</a>", "http://jsoup.org/");
            Element a = doc.Select("a").First;
            Assert.AreEqual("/foo", a.Attr("href"));
            Assert.AreEqual("http://jsoup.org/foo", a.Attr("abs:href"));
            Assert.IsFalse(a.HasAttr("abs:href")); // only realised on the get method, not in has or iterator
        }

        /*
    Test for an issue with Java's abs URL handler.
     */
        [TestMethod]
        public void absHandlesRelativeQuery()
        {
            Document doc = NSoup.NSoupClient.Parse("<a href='?foo'>One</a> <a href='bar.html?foo'>Two</a>", "http://jsoup.org/path/file?bar");

            Element a1 = doc.Select("a").First;
            Assert.AreEqual("http://jsoup.org/path/file?foo", a1.AbsUrl("href"));

            Element a2 = doc.Select("a")[1];
            Assert.AreEqual("http://jsoup.org/path/bar.html?foo", a2.AbsUrl("href"));
        }

        [TestMethod]
        public void testRemove()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>One <span>two</span> three</p>");
            Element p = doc.Select("p").First;
            p.ChildNodes[0].Remove();

            Assert.AreEqual("two three", p.Text());
            Assert.AreEqual("<span>two</span> three", TextUtil.StripNewLines(p.Html()));
        }

        [TestMethod]
        public void testReplace()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>One <span>two</span> three</p>");
            Element p = doc.Select("p").First;
            Element insert = doc.CreateElement("em");
            insert.Text("foo");
            p.ChildNodes[1].ReplaceWith(insert);

            Assert.AreEqual("One <em>foo</em> three", p.Html());
        }

        [TestMethod]
        public void ownerDocument()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>Hello");
            Element p = doc.Select("p").First;
            Assert.IsTrue(p.OwnerDocument == doc);
            Assert.IsTrue(doc.OwnerDocument == doc);
            Assert.IsNull(doc.Parent);
        }
    }
}
