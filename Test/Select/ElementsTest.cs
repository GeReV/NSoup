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
    /// Tests for ElementList.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class ElementsTest
    {
        public ElementsTest()
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

        #region Additional test Attributes
        //
        // You can use the following additional Attributes as you write your tests:
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
        public void filter()
        {
            string h = "<p>Excl</p><div class=headline><p>Hello</p><p>There</p></div><div class=headline><h1>Headline</h1></div>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Elements els = doc.Select(".headline").Select("p");
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual("Hello", els[0].Text());
            Assert.AreEqual("There", els[1].Text());
        }

        [TestMethod]
        public void Attributes()
        {
            string h = "<p title=foo><p title=bar><p class=foo><p class=bar>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Elements withTitle = doc.Select("p[title]");
            Assert.AreEqual(2, withTitle.Count);
            Assert.IsTrue(withTitle.HasAttr("title"));
            Assert.IsFalse(withTitle.HasAttr("class"));
            Assert.AreEqual("foo", withTitle.Attr("title"));

            withTitle.RemoveAttr("title");
            Assert.AreEqual(2, withTitle.Count); // existing Elements are not reevaluated
            Assert.AreEqual(0, doc.Select("p[title]").Count);

            Elements ps = doc.Select("p").Attr("style", "classy");
            Assert.AreEqual(4, ps.Count);
            Assert.AreEqual("classy", ps.Last.Attr("style"));
            Assert.AreEqual("bar", ps.Last.Attr("class"));
        }

        [TestMethod]
        public void HasAttr()
        {
            Document doc = NSoup.NSoupClient.Parse("<p title=foo><p title=bar><p class=foo><p class=bar>");
            Elements ps = doc.Select("p");
            Assert.IsTrue(ps.HasAttr("class"));
            Assert.IsFalse(ps.HasAttr("style"));
        }

        [TestMethod]
        public void Attr()
        {
            Document doc = NSoup.NSoupClient.Parse("<p title=foo><p title=bar><p class=foo><p class=bar>");
            string classVal = doc.Select("p").Attr("class");
            Assert.AreEqual("foo", classVal);
        }

        [TestMethod]
        public void classes()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p class='mellow yellow'></p><p class='red green'></p>");

            Elements els = doc.Select("p");
            Assert.IsTrue(els.HasClass("red"));
            Assert.IsFalse(els.HasClass("blue"));
            els.AddClass("blue");
            els.RemoveClass("yellow");
            els.ToggleClass("mellow");

            Assert.AreEqual("blue", els[0].ClassName());
            Assert.AreEqual("red green blue mellow", els[1].ClassName());
        }

        [TestMethod]
        public void text()
        {
            string h = "<div><p>Hello<p>there<p>world</div>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("Hello there world", doc.Select("div > *").Text);
        }

        [TestMethod]
        public void hasText()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p></div><div><p></p></div>");
            Elements divs = doc.Select("div");
            Assert.IsTrue(divs.HasText);
            Assert.IsFalse(doc.Select("div + div").HasText);
        }

        [TestMethod]
        public void html()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p></div><div><p>There</p></div>");
            Elements divs = doc.Select("div");
            Assert.AreEqual("<p>Hello</p>\n<p>There</p>", divs.Html());
        }

        [TestMethod]
        public void outerHtml()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p></div><div><p>There</p></div>");
            Elements divs = doc.Select("div");
            Assert.AreEqual("<div><p>Hello</p></div><div><p>There</p></div>", TextUtil.StripNewLines(divs.OuterHtml()));
        }

        [TestMethod]
        public void setHtml()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>One</p><p>Two</p><p>Three</p>");
            Elements ps = doc.Select("p");

            ps.Prepend("<b>Bold</b>").Append("<i>Ital</i>");
            Assert.AreEqual("<p><b>Bold</b>Two<i>Ital</i></p>", TextUtil.StripNewLines(ps[1].OuterHtml()));

            ps.Html("<span>Gone</span>");
            Assert.AreEqual("<p><span>Gone</span></p>", TextUtil.StripNewLines(ps[1].OuterHtml()));
        }

        [TestMethod]
        public void val()
        {
            Document doc = NSoup.NSoupClient.Parse("<input value='one' /><textarea>two</textarea>");
            Elements els = doc.Select("form > *");
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual("one", els.Val());
            Assert.AreEqual("two", els.Last.Val());

            els.Val("three");
            Assert.AreEqual("three", els.First.Val());
            Assert.AreEqual("three", els.Last.Val());
            Assert.AreEqual("<textarea>three</textarea>", els.Last.OuterHtml());
        }

        [TestMethod]
        public void before()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>This <a>is</a> <a>jsoup</a>.</p>");
            doc.Select("a").Before("<span>foo</span>");
            Assert.AreEqual("<p>This <span>foo</span><a>is</a> <span>foo</span><a>jsoup</a>.</p>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void after()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>This <a>is</a> <a>jsoup</a>.</p>");
            doc.Select("a").after("<span>foo</span>");
            Assert.AreEqual("<p>This <a>is</a><span>foo</span> <a>jsoup</a><span>foo</span>.</p>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void wrap()
        {
            string h = "<p><b>This</b> is <b>jsoup</b></p>";
            Document doc = NSoup.NSoupClient.Parse(h);
            doc.Select("b").Wrap("<i></i>");
            Assert.AreEqual("<p><i><b>This</b></i> is <i><b>jsoup</b></i></p>", doc.Body.Html());
        }

        [TestMethod]
        public void empty()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello <b>there</b></p> <p>now!</p></div>");
            doc.GetOutputSettings().PrettyPrint(false);

            doc.Select("p").Empty();
            Assert.AreEqual("<div><p></p> <p></p></div>", doc.Body.Html());
        }

        [TestMethod]
        public void remove()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello <b>there</b></p> jsoup <p>now!</p></div>");
            doc.GetOutputSettings().PrettyPrint(false);

            doc.Select("p").Remove();
            Assert.AreEqual("<div> jsoup </div>", doc.Body.Html());
        }

        [TestMethod]
        public void eq()
        {
            string h = "<p>Hello<p>there<p>world";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("there", doc.Select("p").Eq(1).Text);
            Assert.AreEqual("there", doc.Select("p")[1].Text());
        }

        [TestMethod]
        public void Is()
        {
            string h = "<p>Hello<p title=foo>there<p>world";
            Document doc = NSoup.NSoupClient.Parse(h);
            Elements ps = doc.Select("p");
            Assert.IsTrue(ps.Is("[title=foo]"));
            Assert.IsFalse(ps.Is("[title=bar]"));
        }

        [TestMethod]
        public void parents()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p></div><p>There</p>");
            Elements parents = doc.Select("p").Parents;

            Assert.AreEqual(3, parents.Count);
            Assert.AreEqual("div", parents[0].TagName());
            Assert.AreEqual("body", parents[1].TagName());
            Assert.AreEqual("html", parents[2].TagName());
        }

        [TestMethod]
        public void not()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1><p>One</p></div> <div id=2><p><span>Two</span></p></div>");

            Elements div1 = doc.Select("div").Not(":has(p > span)");
            Assert.AreEqual(1, div1.Count);
            Assert.AreEqual("1", div1.First.Id);

            Elements div2 = doc.Select("div").Not("#1");
            Assert.AreEqual(1, div2.Count);
            Assert.AreEqual("2", div2.First.Id);
        }

        [TestMethod]
        public void tagNameSet()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>Hello <i>there</i> <i>now</i></p>");
            doc.Select("i").TagName("em");

            Assert.AreEqual("<p>Hello <em>there</em> <em>now</em></p>", doc.Body.Html());
        }
    }
}
