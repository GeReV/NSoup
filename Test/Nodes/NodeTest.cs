using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Parse;
using NSoup;
using NSoup.Select;

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
        public void setBaseUriIsRecursive()
        {
            Document doc = NSoupClient.Parse("<div><p></p></div>");
            string baseUri = "http://jsoup.org";
            doc.BaseUri = baseUri;

            Assert.AreEqual(baseUri, doc.BaseUri);
            Assert.AreEqual(baseUri, doc.Select("div").First.BaseUri);
            Assert.AreEqual(baseUri, doc.Select("p").First.BaseUri);
        }

        [TestMethod]
        public void handlesAbsPrefix()
        {
            Document doc = NSoup.NSoupClient.Parse("<a href=/foo>Hello</a>", "http://jsoup.org/");
            Element a = doc.Select("a").First;
            Assert.AreEqual("/foo", a.Attr("href"));
            Assert.AreEqual("http://jsoup.org/foo", a.Attr("abs:href"));
            Assert.IsTrue(a.HasAttr("abs:href"));
        }

        [TestMethod]
        public void handlesAbsOnImage()
        {
            Document doc = NSoup.NSoupClient.Parse("<p><img src=\"/rez/osi_logo.png\" /></p>", "http://jsoup.org/");
            Element img = doc.Select("img").First;
            Assert.AreEqual("http://jsoup.org/rez/osi_logo.png", img.Attr("abs:src"));
            Assert.AreEqual(img.AbsUrl("src"), img.Attr("abs:src"));
        }

        [TestMethod]
        public void handlesAbsPrefixOnHasAttr()
        {
            // 1: no abs url; 2: has abs url
            Document doc = NSoup.NSoupClient.Parse("<a id=1 href='/foo'>One</a> <a id=2 href='http://jsoup.org/'>Two</a>");
            Element one = doc.Select("#1").First;
            Element two = doc.Select("#2").First;

            Assert.IsFalse(one.HasAttr("abs:href"));
            Assert.IsTrue(one.HasAttr("href"));
            Assert.AreEqual("", one.AbsUrl("href"));

            Assert.IsTrue(two.HasAttr("abs:href"));
            Assert.IsTrue(two.HasAttr("href"));
            Assert.AreEqual("http://jsoup.org/", two.AbsUrl("href"));
        }

        [TestMethod]
        public void literalAbsPrefix()
        {
            // if there is a literal attribute "abs:xxx", don't try and make absolute.
            Document doc = NSoup.NSoupClient.Parse("<a abs:href='odd'>One</a>");
            Element el = doc.Select("a").First;
            Assert.IsTrue(el.HasAttr("abs:href"));
            Assert.AreEqual("odd", el.Attr("abs:href"));
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

        [TestMethod]
        public void before()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>One <b>two</b> three</p>");
            Element newNode = new Element(Tag.ValueOf("em"), "");
            newNode.AppendText("four");

            doc.Select("b").First.Before(newNode);
            Assert.AreEqual("<p>One <em>four</em><b>two</b> three</p>", doc.Body.Html());

            doc.Select("b").First.Before("<i>five</i>");
            Assert.AreEqual("<p>One <em>four</em><i>five</i><b>two</b> three</p>", doc.Body.Html());
        }

        [TestMethod]
        public void after()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>One <b>two</b> three</p>");
            Element newNode = new Element(Tag.ValueOf("em"), "");
            newNode.AppendText("four");

            doc.Select("b").First.After(newNode);
            Assert.AreEqual("<p>One <b>two</b><em>four</em> three</p>", doc.Body.Html());

            doc.Select("b").First.After("<i>five</i>");
            Assert.AreEqual("<p>One <b>two</b><i>five</i><em>four</em> three</p>", doc.Body.Html());
        }

        [TestMethod]
        public void unwrap()
        {
            Document doc = NSoup.NSoupClient.Parse("<div>One <span>Two <b>Three</b></span> Four</div>");
            Element span = doc.Select("span").First;
            Node twoText = span.ChildNodes[0];
            Node node = span.Unwrap();

            Assert.AreEqual("<div>One Two <b>Three</b> Four</div>", TextUtil.StripNewLines(doc.Body.Html()));
            Assert.IsTrue(node is TextNode);
            Assert.AreEqual("Two ", ((TextNode)node).Text());
            Assert.AreEqual(node, twoText);
            Assert.AreEqual(node.ParentNode, doc.Select("div").First);
        }

        [TestMethod]
        public void unwrapNoChildren()
        {
            Document doc = NSoup.NSoupClient.Parse("<div>One <span></span> Two</div>");
            Element span = doc.Select("span").First();
            Node node = span.Unwrap();
            Assert.AreEqual("<div>One  Two</div>", TextUtil.StripNewLines(doc.Body.Html()));
            Assert.IsTrue(node == null);
        }

        [TestMethod]
        public void traverse()
        {
            Document doc = NSoupClient.Parse("<div><p>Hello</p></div><div>There</div>");
            StringBuilder accum = new StringBuilder();
            doc.Select("div").First.Traverse(new TestNodeVisitor(accum));
            Assert.AreEqual("<div><p><#text></#text></p></div>", accum.ToString());
        }

        private class TestNodeVisitor : NodeVisitor
        {
            StringBuilder accum;

            public TestNodeVisitor(StringBuilder accum)
            {
                this.accum = accum;
            }

            public void Head(Node node, int depth)
            {
                accum.Append("<" + node.NodeName + ">");
            }

            public void Tail(Node node, int depth)
            {
                accum.Append("</" + node.NodeName + ">");
            }
        }

        [TestMethod]
        public void orphanNodeReturnsNullForSiblingElements()
        {
            Node node = new Element(Tag.ValueOf("p"), "");
            Element el = new Element(Tag.ValueOf("p"), "");

            Assert.AreEqual(0, node.SiblingIndex);
            Assert.AreEqual(0, node.SiblingNodes.Count);

            Assert.IsNull(node.PreviousSibling);
            Assert.IsNull(node.NextSibling);

            Assert.AreEqual(0, el.SiblingElements.Count);
            Assert.IsNull(el.PreviousElementSibling);
            Assert.IsNull(el.NextElementSibling);
        }

        [TestMethod]
        public void nodeIsNotASiblingOfItself()
        {
            Document doc = NSoupClient.Parse("<div><p>One<p>Two<p>Three</div>");
            Element p2 = doc.Select("p")[1];

            Assert.AreEqual("Two", p2.Text());
            IList<Node> nodes = p2.SiblingNodes;
            Assert.AreEqual(2, nodes.Count);
            Assert.AreEqual("<p>One</p>", nodes[0].OuterHtml());
            Assert.AreEqual("<p>Three</p>", nodes[1].OuterHtml());
        }
    }
}
