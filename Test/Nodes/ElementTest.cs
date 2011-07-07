using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Select;

namespace Test.Nodes
{
    /// <summary>
    /// Tests for Element (DOM stuff mostly).
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class ElementTest
    {
        public ElementTest()
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

        private string reference = "<div id=div1><p>Hello</p><p>Another <b>element</b></p><div id=div2><img src=foo.png></div></div>";

        [TestMethod]
        public void getElementsByTagName()
        {
            Document doc = NSoup.NSoupClient.Parse(reference);
            List<Element> divs = doc.GetElementsByTag("div").ToList();
            Assert.AreEqual(2, divs.Count);
            Assert.AreEqual("div1", divs[0].Id);
            Assert.AreEqual("div2", divs[1].Id);

            List<Element> ps = doc.GetElementsByTag("p").ToList();
            Assert.AreEqual(2, ps.Count);
            Assert.AreEqual("Hello", ((TextNode)ps[0].ChildNodes[0]).GetWholeText());
            Assert.AreEqual("Another ", ((TextNode)ps[1].ChildNodes[0]).GetWholeText());
            List<Element> ps2 = doc.GetElementsByTag("P").ToList();
            Assert.IsTrue(ps.SequenceEqual(ps2));

            List<Element> imgs = doc.GetElementsByTag("img").ToList();
            Assert.AreEqual("foo.png", imgs[0].Attr("src"));

            List<Element> empty = doc.GetElementsByTag("wtf").ToList();
            Assert.AreEqual(0, empty.Count);
        }

        [TestMethod]
        public void getNamespacedElementsByTag()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><abc:def id=1>Hello</abc:def></div>");
            Elements els = doc.GetElementsByTag("abc:def");
            Assert.AreEqual(1, els.Count);
            Assert.AreEqual("1", els.First.Id);
            Assert.AreEqual("abc:def", els.First.TagName());
        }

        [TestMethod]
        public void testGetElementById()
        {
            Document doc = NSoup.NSoupClient.Parse(reference);
            Element div = doc.GetElementById("div1");
            Assert.AreEqual("div1", div.Id);
            Assert.IsNull(doc.GetElementById("none"));

            Document doc2 = NSoup.NSoupClient.Parse("<div id=1><div id=2><p>Hello <span id=2>world!</span></p></div></div>");
            Element div2 = doc2.GetElementById("2");
            Assert.AreEqual("div", div2.TagName()); // not the span
            Element span = div2.Child(0).GetElementById("2"); // called from <p> context should be span
            Assert.AreEqual("span", span.TagName());
        }

        [TestMethod]
        public void testGetText()
        {
            Document doc = NSoup.NSoupClient.Parse(reference);
            Assert.AreEqual("Hello Another element", doc.Text());
            Assert.AreEqual("Another element", doc.GetElementsByTag("p")[1].Text());
        }

        [TestMethod]
        public void testGetChildText()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>Hello <b>there</b> now");
            Element p = doc.Select("p").First;
            Assert.AreEqual("Hello there now", p.Text());
            Assert.AreEqual("Hello now", p.OwnText());
        }

        [TestMethod]
        public void testNormalisesText()
        {
            string h = "<p>Hello<p>There.</p> \n <p>Here <b>is</b> \n s<b>om</b>e text.";
            Document doc = NSoup.NSoupClient.Parse(h);
            string text = doc.Text();
            Assert.AreEqual("Hello There. Here is some text.", text);
        }

        [TestMethod]
        public void testKeepsPreText()
        {
            string h = "<p>Hello \n \n there.</p> <div><pre>  What's \n\n  that?</pre>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("Hello there.   What's \n\n  that?", doc.Text());
        }

        [TestMethod]
        public void testKeepsPreTextInCode()
        {
            string h = "<pre><code>code\n\ncode</code></pre>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("code\n\ncode", doc.Text());
            Assert.AreEqual("<pre><code>code\n\ncode</code></pre>", doc.Body.Html());
        }

        [TestMethod]
        public void testBrHasSpace()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>Hello<br>there</p>");
            Assert.AreEqual("Hello there", doc.Text());
            Assert.AreEqual("Hello there", doc.Select("p").First.OwnText());

            doc = NSoup.NSoupClient.Parse("<p>Hello <br> there</p>");
            Assert.AreEqual("Hello there", doc.Text());
        }

        [TestMethod]
        public void testGetSiblings()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello<p id=1>there<p>this<p>is<p>an<p id=last>element</div>");
            Element p = doc.GetElementById("1");
            Assert.AreEqual("there", p.Text());
            Assert.AreEqual("Hello", p.PreviousElementSibling.Text());
            Assert.AreEqual("this", p.NextElementSibling.Text());
            Assert.AreEqual("Hello", p.FirstElementSibling.Text());
            Assert.AreEqual("element", p.LastElementSibling.Text());
        }

        [TestMethod]
        public void testGetParents()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello <span>there</span></div>");
            Element span = doc.Select("span").First;
            Elements parents = span.Parents;

            Assert.AreEqual(4, parents.Count);
            Assert.AreEqual("p", parents[0].TagName());
            Assert.AreEqual("div", parents[1].TagName());
            Assert.AreEqual("body", parents[2].TagName());
            Assert.AreEqual("html", parents[3].TagName());
        }

        [TestMethod]
        public void testElementSiblingIndex()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>One</p>...<p>Two</p>...<p>Three</p>");
            Elements ps = doc.Select("p");
            Assert.IsTrue(0 == ps[0].ElementSiblingIndex);
            Assert.IsTrue(1 == ps[1].ElementSiblingIndex);
            Assert.IsTrue(2 == ps[2].ElementSiblingIndex);
        }

        [TestMethod]
        public void testGetElementsWithClass()
        {
            Document doc = NSoup.NSoupClient.Parse("<div class='mellow yellow'><span class=mellow>Hello <b class='yellow'>Yellow!</b></span><p>Empty</p></div>");

            List<Element> els = doc.GetElementsByClass("mellow").ToList();
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual("div", els[0].TagName());
            Assert.AreEqual("span", els[1].TagName());

            List<Element> els2 = doc.GetElementsByClass("yellow").ToList();
            Assert.AreEqual(2, els2.Count);
            Assert.AreEqual("div", els2[0].TagName());
            Assert.AreEqual("b", els2[1].TagName());

            List<Element> none = doc.GetElementsByClass("solo").ToList();
            Assert.AreEqual(0, none.Count);
        }

        [TestMethod]
        public void testGetElementsWithAttribute()
        {
            Document doc = NSoup.NSoupClient.Parse("<div style='bold'><p title=qux><p><b style></b></p></div>");
            List<Element> els = doc.GetElementsByAttribute("style").ToList();
            Assert.AreEqual(2, els.Count);
            Assert.AreEqual("div", els[0].TagName());
            Assert.AreEqual("b", els[1].TagName());

            List<Element> none = doc.GetElementsByAttribute("class").ToList();
            Assert.AreEqual(0, none.Count);
        }

        [TestMethod]
        public void testGetElementsWithAttributeDash()
        {
            Document doc = NSoup.NSoupClient.Parse("<meta http-equiv=content-type value=utf8 id=1> <meta name=foo content=bar id=2> <div http-equiv=content-type value=utf8 id=3>");
            Elements meta = doc.Select("meta[http-equiv=content-type], meta[charset]");
            Assert.AreEqual(1, meta.Count);
            Assert.AreEqual("1", meta.First.Id);
        }

        [TestMethod]
        public void testGetElementsWithAttributeValue()
        {
            Document doc = NSoup.NSoupClient.Parse("<div style='bold'><p><p><b style></b></p></div>");
            List<Element> els = doc.GetElementsByAttributeValue("style", "bold").ToList();
            Assert.AreEqual(1, els.Count);
            Assert.AreEqual("div", els[0].TagName());

            List<Element> none = doc.GetElementsByAttributeValue("style", "none").ToList();
            Assert.AreEqual(0, none.Count);
        }

        [TestMethod]
        public void testClassDomMethods()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><span class='mellow yellow'>Hello <b>Yellow</b></span></div>");
            List<Element> els = doc.GetElementsByAttribute("class").ToList();
            Element span = els[0];
            Assert.AreEqual("mellow yellow", span.ClassName());
            Assert.IsTrue(span.HasClass("mellow"));
            Assert.IsTrue(span.HasClass("yellow"));
            HashSet<string> classes = span.ClassNames();
            Assert.AreEqual(2, classes.Count);
            Assert.IsTrue(classes.Contains("mellow"));
            Assert.IsTrue(classes.Contains("yellow"));

            Assert.AreEqual("", doc.ClassName());
            Assert.IsFalse(doc.HasClass("mellow"));
        }

        [TestMethod]
        public void testClassUpdates()
        {
            Document doc = NSoup.NSoupClient.Parse("<div class='mellow yellow'></div>");
            Element div = doc.Select("div").First;

            div.AddClass("green");
            Assert.AreEqual("mellow yellow green", div.ClassName());
            div.RemoveClass("red"); // noop
            div.RemoveClass("yellow");
            Assert.AreEqual("mellow green", div.ClassName());
            div.ToggleClass("green").ToggleClass("red");
            Assert.AreEqual("mellow red", div.ClassName());
        }

        [TestMethod]
        public void testOuterHtml()
        {
            Document doc = NSoup.NSoupClient.Parse("<div title='Tags &amp;c.'><img src=foo.png><p><!-- comment -->Hello<p>there");
            Assert.AreEqual("<html><head></head><body><div title=\"Tags &amp;c.\"><img src=\"foo.png\" /><p><!-- comment -->Hello</p><p>there</p></div></body></html>",
                    TextUtil.StripNewLines(doc.OuterHtml()));
        }

        [TestMethod]
        public void testInnerHtml()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p></div>");
            Assert.AreEqual("<p>Hello</p>", doc.GetElementsByTag("div")[0].Html());
        }

        [TestMethod]
        public void testFormatHtml()
        {
            Document doc = NSoup.NSoupClient.Parse("<title>Format test</title><div><p>Hello <span>jsoup <span>users</span></span></p><p>Good.</p></div>");
            Assert.AreEqual("<html>\n <head>\n  <title>Format test</title>\n </head>\n <body>\n  <div>\n   <p>Hello <span>jsoup <span>users</span></span></p>\n   <p>Good.</p>\n  </div>\n </body>\n</html>", doc.Html());
        }

        [TestMethod]
        public void testSetIndent()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello\nthere</p></div>");
            doc.Settings.IndentAmount(0);
            Assert.AreEqual("<html>\n<head></head>\n<body>\n<div>\n<p>Hello there</p>\n</div>\n</body>\n</html>", doc.Html());
        }

        [TestMethod]
        public void testNotPretty()
        {
            Document doc = NSoup.NSoupClient.Parse("<div>   \n<p>Hello\n there</p></div>");
            doc.Settings.PrettyPrint(false);
            Assert.AreEqual("<html><head></head><body><div>   \n<p>Hello\n there</p></div></body></html>", doc.Html());
        }

        [TestMethod]
        public void testEmptyElementFormatHtml()
        {
            // don't put newlines into empty blocks
            Document doc = NSoup.NSoupClient.Parse("<section><div></div></section>");
            Assert.AreEqual("<section>\n <div></div>\n</section>", doc.Select("section").First.OuterHtml());
        }

        [TestMethod]
        public void testContainerOutput()
        {
            Document doc = NSoup.NSoupClient.Parse("<title>Hello there</title> <div><p>Hello</p><p>there</p></div> <div>Another</div>");
            Assert.AreEqual("<title>Hello there</title>", doc.Select("title").First.OuterHtml());
            Assert.AreEqual("<div>\n <p>Hello</p>\n <p>there</p>\n</div>", doc.Select("div").First.OuterHtml());
            Assert.AreEqual("<div>\n <p>Hello</p>\n <p>there</p>\n</div> \n<div>\n Another\n</div>", doc.Select("body").First.Html());
        }

        [TestMethod]
        public void testSetText()
        {
            string h = "<div id=1>Hello <p>there <b>now</b></p></div>";
            Document doc = NSoup.NSoupClient.Parse(h);
            Assert.AreEqual("Hello there now", doc.Text()); // need to sort out node whitespace
            Assert.AreEqual("there now", doc.Select("p")[0].Text());

            Element div = doc.GetElementById("1").Text("Gone");
            Assert.AreEqual("Gone", div.Text());
            Assert.AreEqual(0, doc.Select("p").Count);
        }

        [TestMethod]
        public void testAddNewElement()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1><p>Hello</p></div>");
            Element div = doc.GetElementById("1");
            div.AppendElement("p").Text("there");
            div.AppendElement("P").Attr("class", "second").Text("now");
            Assert.AreEqual("<html><head></head><body><div id=\"1\"><p>Hello</p><p>there</p><p class=\"second\">now</p></div></body></html>",
                    TextUtil.StripNewLines(doc.Html()));

            // check sibling index (with short circuit on reindexChildren):
            Elements ps = doc.Select("p");
            for (int i = 0; i < ps.Count; i++)
            {
                Assert.AreEqual(i, ps[i].SiblingIndex);
            }
        }

        [TestMethod]
        public void testAppendRowToTable()
        {
            Document doc = NSoup.NSoupClient.Parse("<table><tr><td>1</td></tr></table>");
            Element table = doc.Select("tbody").First;
            table.Append("<tr><td>2</td></tr>");

            Assert.AreEqual("<table><tbody><tr><td>1</td></tr><tr><td>2</td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));

            // check sibling index (reindexChildren):
            Elements ps = doc.Select("tr");
            for (int i = 0; i < ps.Count; i++)
            {
                Assert.AreEqual(i, ps[i].SiblingIndex);
            }
        }

        [TestMethod]
        public void testPrependRowToTable()
        {
            Document doc = NSoup.NSoupClient.Parse("<table><tr><td>1</td></tr></table>");
            Element table = doc.Select("tbody").First();
            table.Prepend("<tr><td>2</td></tr>");

            Assert.AreEqual("<table><tbody><tr><td>2</td></tr><tr><td>1</td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testPrependElement()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1><p>Hello</p></div>");
            Element div = doc.GetElementById("1");
            div.PrependElement("p").Text("Before");
            Assert.AreEqual("Before", div.Child(0).Text());
            Assert.AreEqual("Hello", div.Child(1).Text());
        }

        [TestMethod]
        public void testAddNewText()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1><p>Hello</p></div>");
            Element div = doc.GetElementById("1");
            div.AppendText(" there & now >");
            Assert.AreEqual("<p>Hello</p> there &amp; now &gt;", TextUtil.StripNewLines(div.Html()));
        }

        [TestMethod]
        public void testPrependText()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1><p>Hello</p></div>");
            Element div = doc.GetElementById("1");
            div.PrependText("there & now > ");
            Assert.AreEqual("there & now > Hello", div.Text());
            Assert.AreEqual("there &amp; now &gt; <p>Hello</p>", TextUtil.StripNewLines(div.Html()));
        }

        [TestMethod]
        public void testAddNewHtml()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1><p>Hello</p></div>");
            Element div = doc.GetElementById("1");
            div.Append("<p>there</p><p>now</p>");
            Assert.AreEqual("<p>Hello</p><p>there</p><p>now</p>", TextUtil.StripNewLines(div.Html()));

            // check sibling index (no reindexChildren):
            Elements ps = doc.Select("p");
            for (int i = 0; i < ps.Count; i++)
            {
                Assert.AreEqual(i, ps[i].SiblingIndex);
            }
        }

        [TestMethod]
        public void testPrependNewHtml()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1><p>Hello</p></div>");
            Element div = doc.GetElementById("1");
            div.Prepend("<p>there</p><p>now</p>");
            Assert.AreEqual("<p>there</p><p>now</p><p>Hello</p>", TextUtil.StripNewLines(div.Html()));

            // check sibling index (reindexChildren):
            Elements ps = doc.Select("p");
            for (int i = 0; i < ps.Count; i++)
            {
                Assert.AreEqual(i, ps[i].SiblingIndex);
            }
        }

        [TestMethod]
        public void testSetHtml()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1><p>Hello</p></div>");
            Element div = doc.GetElementById("1");
            div.Html("<p>there</p><p>now</p>");
            Assert.AreEqual("<p>there</p><p>now</p>", TextUtil.StripNewLines(div.Html()));
        }

        [TestMethod]
        public void testWrap()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p><p>There</p></div>");
            Element p = doc.Select("p").First;
            p.Wrap("<div class='head'></div>");
            Assert.AreEqual("<div><div class=\"head\"><p>Hello</p></div><p>There</p></div>", TextUtil.StripNewLines(doc.Body.Html()));

            Element ret = p.Wrap("<div><div class=foo></div><p>What?</p></div>");
            Assert.AreEqual("<div><div class=\"head\"><div><div class=\"foo\"><p>Hello</p></div><p>What?</p></div></div><p>There</p></div>",
                    TextUtil.StripNewLines(doc.Body.Html()));

            Assert.AreEqual(ret, p);
        }

        [TestMethod]
        public void before()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p><p>There</p></div>");
            Element p1 = doc.Select("p").First;
            p1.Before("<div>one</div><div>two</div>");
            Assert.AreEqual("<div><div>one</div><div>two</div><p>Hello</p><p>There</p></div>", TextUtil.StripNewLines(doc.Body.Html()));

            doc.Select("p").Last.Before("<p>Three</p><!-- four -->");
            Assert.AreEqual("<div><div>one</div><div>two</div><p>Hello</p><p>Three</p><!-- four --><p>There</p></div>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void after()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p><p>There</p></div>");
            Element p1 = doc.Select("p").First;
            p1.After("<div>one</div><div>two</div>");
            Assert.AreEqual("<div><p>Hello</p><div>one</div><div>two</div><p>There</p></div>", TextUtil.StripNewLines(doc.Body.Html()));

            doc.Select("p").Last.After("<p>Three</p><!-- four -->");
            Assert.AreEqual("<div><p>Hello</p><div>one</div><div>two</div><p>There</p><p>Three</p><!-- four --></div>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testWrapWithRemainder()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p></div>");
            Element p = doc.Select("p").First;
            p.Wrap("<div class='head'></div><p>There!</p>");
            Assert.AreEqual("<div><div class=\"head\"><p>Hello</p><p>There!</p></div></div>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testHasText()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>Hello</p><p></p></div>");
            Element div = doc.Select("div").First;
            Elements ps = doc.Select("p");

            Assert.IsTrue(div.HasText);
            Assert.IsTrue(ps.First.HasText);
            Assert.IsFalse(ps.Last.HasText);
        }

        [TestMethod]
        public void dataset()
        {
            Document doc = NSoup.NSoupClient.Parse("<div id=1 data-name=jsoup class=new data-package=jar>Hello</div><p id=2>Hello</p>");
            Element div = doc.Select("div").First;
            IDictionary<string, string> dataset = div.Dataset;
            Attributes attributes = div.Attributes;

            // size, get, set, add, remove
            Assert.AreEqual(2, dataset.Count);
            Assert.AreEqual("jsoup", dataset["name"]);
            Assert.AreEqual("jar", dataset["package"]);

            //dataset.Add("name", "jsoup updated");
            dataset["name"] = "jsoup updated"; // Changed to this because by definition, .NET dictionaries are supposed to throw exceptions when adding duplicate values.
            dataset.Add("language", "java");
            dataset.Remove("package");

            Assert.AreEqual(2, dataset.Count);
            Assert.AreEqual(4, attributes.Count);
            Assert.AreEqual("jsoup updated", attributes["data-name"]);
            Assert.AreEqual("jsoup updated", dataset["name"]);
            Assert.AreEqual("java", attributes["data-language"]);
            Assert.AreEqual("java", dataset["language"]);

            attributes.Add("data-food", "bacon");
            Assert.AreEqual(3, dataset.Count);
            Assert.AreEqual("bacon", dataset["food"]);

            attributes.Add("data-", "empty");
            Assert.AreEqual(null, dataset[""]); // data- is not a data attribute

            Element p = doc.Select("p").First;
            Assert.AreEqual(0, p.Dataset.Count);

        }

        [TestMethod]
        public void parentlessToString()
        {
            Document doc = NSoup.NSoupClient.Parse("<img src='foo'>");
            Element img = doc.Select("img").First;
            Assert.AreEqual("<img src=\"foo\" />", img.ToString());

            img.Remove(); // lost its parent
            Assert.AreEqual("<img src=\"foo\" />", img.ToString());
        }

        [TestMethod]
        public void testClone()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><p>One<p><span>Two</div>");

            Element p = doc.Select("p")[1];
            Element clone = (Element)p.Clone();

            Assert.IsNull(clone.Parent); // should be orphaned
            Assert.AreEqual(0, clone.SiblingIndex);
            Assert.AreEqual(1, p.SiblingIndex);
            Assert.IsNotNull(p.Parent);

            clone.Append("<span>Three");
            Assert.AreEqual("<p><span>Two</span><span>Three</span></p>", TextUtil.StripNewLines(clone.OuterHtml()));
            Assert.AreEqual("<div><p>One</p><p><span>Two</span></p></div>", TextUtil.StripNewLines(doc.Body.Html())); // not modified

            doc.Body.AppendChild(clone); // adopt
            Assert.IsNotNull(clone.Parent);
            Assert.AreEqual("<div><p>One</p><p><span>Two</span></p></div><p><span>Two</span><span>Three</span></p>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testTagNameSet()
        {
            Document doc = NSoup.NSoupClient.Parse("<div><i>Hello</i>");
            doc.Select("i").First.TagName("em");
            Assert.AreEqual(0, doc.Select("i").Count);
            Assert.AreEqual(1, doc.Select("em").Count);
            Assert.AreEqual("<em>Hello</em>", doc.Select("div").First.Html());
        }

        [TestMethod]
        public void testHtmlContainsOuter()
        {
            Document doc = NSoup.NSoupClient.Parse("<title>Check</title> <div>Hello there</div>");
            doc.GetOutputSettings().IndentAmount(0);
            Assert.IsTrue(doc.Html().Contains(doc.Select("title").OuterHtml()));
            Assert.IsTrue(doc.Html().Contains(doc.Select("div").OuterHtml()));
        }
    }
}
