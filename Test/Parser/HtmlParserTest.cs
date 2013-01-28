using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Select;
using NSoup;
using NSoup.Parse;
using NSoup.Helper;

namespace Test.Parser
{
    /// <summary>
    /// Test suite for attribute parser.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class HtmlParserTest
    {
        public HtmlParserTest()
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
        public void parsesSimpleDocument()
        {
            string html = "<html><head><title>First!</title></head><body><p>First post! <img src=\"foo.png\" /></p></body></html>";
            Document doc = NSoupClient.Parse(html);
            // need a better way to verify these:
            Element p = doc.Body.Child(0);
            Assert.AreEqual("p", p.TagName());
            Element img = p.Child(0);
            Assert.AreEqual("foo.png", img.Attr("src"));
            Assert.AreEqual("img", img.TagName());
        }

        [TestMethod]
        public void parsesRoughAttributes()
        {
            string html = "<html><head><title>First!</title></head><body><p class=\"foo > bar\">First post! <img src=\"foo.png\" /></p></body></html>";
            Document doc = NSoupClient.Parse(html);

            // need a better way to verify these:
            Element p = doc.Body.Child(0);
            Assert.AreEqual("p", p.TagName());
            Assert.AreEqual("foo > bar", p.Attr("class"));
        }

        [TestMethod]
        public void parsesQuiteRoughAttributes()
        {
            string html = "<p =a>One<a <p>Something</p>Else";
            // this gets a <p> with attr '=a' and an <a tag with an attribue named '<p'; and then auto-recreated
            Document doc = NSoupClient.Parse(html);
            Assert.AreEqual("<p =a=\"\">One<a <p=\"\">Something</a></p>\n" +
                    "<a <p=\"\">Else</a>", doc.Body.Html());

            doc = NSoupClient.Parse("<p .....>");
            Assert.AreEqual("<p .....=\"\"></p>", doc.Body.Html());
        }

        [TestMethod]
        public void parsesComments()
        {
            string html = "<html><head></head><body><img src=foo><!-- <table><tr><td></table> --><p>Hello</p></body></html>";
            Document doc = NSoupClient.Parse(html);

            Element body = doc.Body;
            Comment comment = (Comment)body.ChildNodes[1]; // comment should not be sub of img, as it's an empty tag
            Assert.AreEqual(" <table><tr><td></table> ", comment.GetData());
            Element p = body.Child(1);
            TextNode text = (TextNode)p.ChildNodes[0];
            Assert.AreEqual("Hello", text.GetWholeText());
        }

        [TestMethod]
        public void parsesUnterminatedComments()
        {
            string html = "<p>Hello<!-- <tr><td>";
            Document doc = NSoupClient.Parse(html);
            Element p = doc.GetElementsByTag("p")[0];
            Assert.AreEqual("Hello", p.Text());
            TextNode text = (TextNode)p.ChildNodes[0];
            Assert.AreEqual("Hello", text.GetWholeText());
            Comment comment = (Comment)p.ChildNodes[1];
            Assert.AreEqual(" <tr><td>", comment.GetData());
        }

        [TestMethod]
        public void dropsUnterminatedTag()
        {
            // NSoupClient used to parse this to <p>, but whatwg, webkit will drop.
            string h1 = "<p";
            Document doc = NSoupClient.Parse(h1);
            Assert.AreEqual(0, doc.GetElementsByTag("p").Count);
            Assert.AreEqual("", doc.Text());

            string h2 = "<div id=1<p id='2'";
            doc = NSoupClient.Parse(h2);
            Assert.AreEqual("", doc.Text());
        }

        [TestMethod]
        public void dropsUnterminatedAttribute()
        {
            // NSoupClient used to parse this to <p id="foo">, but whatwg, webkit will drop.
            string h1 = "<p id=\"foo";
            Document doc = NSoupClient.Parse(h1);
            Assert.AreEqual("", doc.Text());
        }

        [TestMethod]
        public void parsesUnterminatedTextarea()
        {
            // don't parse right to end, but break on <p>
            Document doc = NSoupClient.Parse("<body><p><textarea>one<p>two");
            Element t = doc.Select("textarea").First;
            Assert.AreEqual("one", t.Text());
            Assert.AreEqual("two", doc.Select("p")[1].Text());
        }

        [TestMethod]
        public void parsesUnterminatedOption()
        {
            // bit weird this -- browsers and spec get stuck in Select until there's a </Select>
            Document doc = NSoupClient.Parse("<body><p><Select><option>One<option>Two</p><p>Three</p>");
            Elements options = doc.Select("option");
            Assert.AreEqual(2, options.Count);
            Assert.AreEqual("One", options.First.Text());
            Assert.AreEqual("TwoThree", options.Last.Text());
        }

        [TestMethod]
        public void testSpaceAfterTag()
        {
            Document doc = NSoupClient.Parse("<div > <a name=\"top\"></a ><p id=1 >Hello</p></div>");
            Assert.AreEqual("<div> <a name=\"top\"></a><p id=\"1\">Hello</p></div>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void createsDocumentStructure()
        {
            string html = "<meta name=keywords /><link rel=stylesheet /><title>NSoupClient</title><p>Hello world</p>";
            Document doc = NSoupClient.Parse(html);
            Element head = doc.Head;
            Element body = doc.Body;

            Assert.AreEqual(1, doc.Children.Count); // root node: contains html node
            Assert.AreEqual(2, doc.Child(0).Children.Count); // html node: head and body
            Assert.AreEqual(3, head.Children.Count);
            Assert.AreEqual(1, body.Children.Count);

            Assert.AreEqual("keywords", head.GetElementsByTag("meta")[0].Attr("name"));
            Assert.AreEqual(0, body.GetElementsByTag("meta").Count);
            Assert.AreEqual("NSoupClient", doc.Title);
            Assert.AreEqual("Hello world", body.Text());
            Assert.AreEqual("Hello world", body.Children[0].Text());
        }

        [TestMethod]
        public void createsStructureFromBodySnippet()
        {
            // the bar baz stuff naturally goes into the body, but the 'foo' goes into root, and the normalisation routine
            // needs to move into the start of the body
            string html = "foo <b>bar</b> baz";
            Document doc = NSoupClient.Parse(html);
            Assert.AreEqual("foo bar baz", doc.Text());

        }

        [TestMethod]
        public void handlesEscapedData()
        {
            string html = "<div title='Surf &amp; Turf'>Reef &amp; Beef</div>";
            Document doc = NSoupClient.Parse(html);
            Element div = doc.GetElementsByTag("div")[0];

            Assert.AreEqual("Surf & Turf", div.Attr("title"));
            Assert.AreEqual("Reef & Beef", div.Text());
        }

        [TestMethod]
        public void handlesDataOnlyTags()
        {
            string t = "<style>font-family: bold</style>";
            IList<Element> tels = NSoupClient.Parse(t).GetElementsByTag("style");
            Assert.AreEqual("font-family: bold", tels[0].Data);
            Assert.AreEqual("", tels[0].Text());

            string s = "<p>Hello</p><script>obj.insert('<a rel=\"none\" />');\ni++;</script><p>There</p>";
            Document doc = NSoupClient.Parse(s);
            Assert.AreEqual("Hello There", doc.Text());
            Assert.AreEqual("obj.insert('<a rel=\"none\" />');\ni++;", doc.Data);
        }

        [TestMethod]
        public void handlesTextAfterData()
        {
            string h = "<html><body>pre <script>inner</script> aft</body></html>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<html><head></head><body>pre <script>inner</script> aft</body></html>", TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void handlesTextArea()
        {
            Document doc = NSoupClient.Parse("<textarea>Hello</textarea>");
            Elements els = doc.Select("textarea");
            Assert.AreEqual("Hello", els.Text);
            Assert.AreEqual("Hello", els.Val());
        }

        [TestMethod]
        public void preservesSpaceInTextArea()
        {
            // preserve because the tag is marked as preserve white space
            Document doc = NSoupClient.Parse("<textarea>\n\tOne\n\tTwo\n\tThree\n</textarea>");
            string expect = "One\n\tTwo\n\tThree"; // the leading and trailing spaces are dropped as a convenience to authors
            Element el = doc.Select("textarea").First;
            Assert.AreEqual(expect, el.Text());
            Assert.AreEqual(expect, el.Val());
            Assert.AreEqual(expect, el.Html());
            Assert.AreEqual("<textarea>\n\t" + expect + "\n</textarea>", el.OuterHtml()); // but preserved in round-trip html
        }

        [TestMethod]
        public void preservesSpaceInScript()
        {
            // preserve because it's content is a data node
            Document doc = NSoupClient.Parse("<script>\nOne\n\tTwo\n\tThree\n</script>");
            string expect = "\nOne\n\tTwo\n\tThree\n";
            Element el = doc.Select("script").First;
            Assert.AreEqual(expect, el.Data);
            Assert.AreEqual("One\n\tTwo\n\tThree", el.Html());
            Assert.AreEqual("<script>" + expect + "</script>", el.OuterHtml());
        }

        [TestMethod]
        public void doesNotCreateImplicitLists()
        {
            // old NSoupClient used to wrap this in <ul>, but that's not to spec
            string h = "<li>Point one<li>Point two";
            Document doc = NSoupClient.Parse(h);
            Elements ol = doc.Select("ul"); // should NOT have created a default ul.
            Assert.AreEqual(0, ol.Count);
            Elements lis = doc.Select("li");
            Assert.AreEqual(2, lis.Count);
            Assert.AreEqual("body", lis.First.Parent.TagName());

            // no fiddling with non-implicit lists
            string h2 = "<ol><li><p>Point the first<li><p>Point the second";
            Document doc2 = NSoupClient.Parse(h2);

            Assert.AreEqual(0, doc2.Select("ul").Count);
            Assert.AreEqual(1, doc2.Select("ol").Count);
            Assert.AreEqual(2, doc2.Select("ol li").Count);
            Assert.AreEqual(2, doc2.Select("ol li p").Count);
            Assert.AreEqual(1, doc2.Select("ol li")[0].Children.Count); // one p in first li
        }

        [TestMethod]
        public void discardsNakedTds()
        {
            // NSoupClient used to make this into an implicit table; but browsers make it into a text run
            string h = "<td>Hello<td><p>There<p>now";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("Hello<p>There</p><p>now</p>", TextUtil.StripNewLines(doc.Body.Html()));
            // <tbody> is introduced if no implicitly creating table, but allows tr to be directly under table
        }

        [TestMethod]
        public void handlesNestedImplicitTable()
        {
            Document doc = NSoupClient.Parse("<table><td>1</td></tr> <td>2</td></tr> <td> <table><td>3</td> <td>4</td></table> <tr><td>5</table>");
            Assert.AreEqual("<table><tbody><tr><td>1</td></tr> <tr><td>2</td></tr> <tr><td> <table><tbody><tr><td>3</td> <td>4</td></tr></tbody></table> </td></tr><tr><td>5</td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesWhatWgExpensesTableExample()
        {
            // http://www.whatwg.org/specs/web-apps/current-work/multipage/tabular-data.html#examples-0
            Document doc = NSoupClient.Parse("<table> <colgroup> <col> <colgroup> <col> <col> <col> <thead> <tr> <th> <th>2008 <th>2007 <th>2006 <tbody> <tr> <th scope=rowgroup> Research and development <td> $ 1,109 <td> $ 782 <td> $ 712 <tr> <th scope=row> Percentage of net sales <td> 3.4% <td> 3.3% <td> 3.7% <tbody> <tr> <th scope=rowgroup> Selling, general, and administrative <td> $ 3,761 <td> $ 2,963 <td> $ 2,433 <tr> <th scope=row> Percentage of net sales <td> 11.6% <td> 12.3% <td> 12.6% </table>");
            Assert.AreEqual("<table> <colgroup> <col /> </colgroup><colgroup> <col /> <col /> <col /> </colgroup><thead> <tr> <th> </th><th>2008 </th><th>2007 </th><th>2006 </th></tr></thead><tbody> <tr> <th scope=\"rowgroup\"> Research and development </th><td> $ 1,109 </td><td> $ 782 </td><td> $ 712 </td></tr><tr> <th scope=\"row\"> Percentage of net sales </th><td> 3.4% </td><td> 3.3% </td><td> 3.7% </td></tr></tbody><tbody> <tr> <th scope=\"rowgroup\"> Selling, general, and administrative </th><td> $ 3,761 </td><td> $ 2,963 </td><td> $ 2,433 </td></tr><tr> <th scope=\"row\"> Percentage of net sales </th><td> 11.6% </td><td> 12.3% </td><td> 12.6% </td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesTbodyTable()
        {
            Document doc = NSoupClient.Parse("<html><head></head><body><table><tbody><tr><td>aaa</td><td>bbb</td></tr></tbody></table></body></html>");
            Assert.AreEqual("<table><tbody><tr><td>aaa</td><td>bbb</td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesImplicitCaptionClose()
        {
            Document doc = NSoupClient.Parse("<table><caption>A caption<td>One<td>Two");
            Assert.AreEqual("<table><caption>A caption</caption><tbody><tr><td>One</td><td>Two</td></tr></tbody></table>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void noTableDirectInTable()
        {
            Document doc = NSoupClient.Parse("<table> <td>One <td><table><td>Two</table> <table><td>Three");
            Assert.AreEqual("<table> <tbody><tr><td>One </td><td><table><tbody><tr><td>Two</td></tr></tbody></table> <table><tbody><tr><td>Three</td></tr></tbody></table></td></tr></tbody></table>",
                    TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void ignoresDupeEndTrTag()
        {
            Document doc = NSoupClient.Parse("<table><tr><td>One</td><td><table><tr><td>Two</td></tr></tr></table></td><td>Three</td></tr></table>"); // two </tr></tr>, must ignore or will close table
            Assert.AreEqual("<table><tbody><tr><td>One</td><td><table><tbody><tr><td>Two</td></tr></tbody></table></td><td>Three</td></tr></tbody></table>",
                    TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesBaseTags()
        {
            // only listen to the first base href
            string h = "<a href=1>#</a><base href='/2/'><a href='3'>#</a><base href='http://bar'><a href=/4>#</a>";
            Document doc = NSoupClient.Parse(h, "http://foo/");
            Assert.AreEqual("http://foo/2/", doc.BaseUri); // gets set once, so doc and descendants have first only

            Elements anchors = doc.GetElementsByTag("a");
            Assert.AreEqual(3, anchors.Count);

            Assert.AreEqual("http://foo/2/", anchors[0].BaseUri);
            Assert.AreEqual("http://foo/2/", anchors[1].BaseUri);
            Assert.AreEqual("http://foo/2/", anchors[2].BaseUri);

            Assert.AreEqual("http://foo/2/1", anchors[0].AbsUrl("href"));
            Assert.AreEqual("http://foo/2/3", anchors[1].AbsUrl("href"));
            Assert.AreEqual("http://foo/4", anchors[2].AbsUrl("href"));
        }

        [TestMethod]
        public void handlesCData()
        {
            // todo: as this is html namespace, should actually treat as bogus comment, not cdata. keep as cdata for now
            string h = "<div id=1><![CDATA[<html>\n<foo><&amp;]]></div>"; // the &amp; in there should remain literal
            Document doc = NSoupClient.Parse(h);
            Element div = doc.GetElementById("1");
            Assert.AreEqual("<html> <foo><&amp;", div.Text());
            Assert.AreEqual(0, div.Children.Count);
            Assert.AreEqual(1, div.ChildNodes.Count); // no elements, one text node
        }

        [TestMethod]
        public void handlesInvalidStartTags()
        {
            string h = "<div>Hello < There <&amp;></div>"; // parse to <div {#text=Hello < There <&>}>
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("Hello < There <&>", doc.Select("div").First.Text());
        }

        [TestMethod]
        public void handlesUnknownTags()
        {
            string h = "<div><foo title=bar>Hello<foo title=qux>there</foo></div>";
            Document doc = NSoupClient.Parse(h);
            Elements foos = doc.Select("foo");
            Assert.AreEqual(2, foos.Count);
            Assert.AreEqual("bar", foos.First.Attr("title"));
            Assert.AreEqual("qux", foos.Last.Attr("title"));
            Assert.AreEqual("there", foos.Last.Text());
        }

        [TestMethod]
        public void handlesUnknownInlineTags()
        {
            string h = "<p><cust>Test</cust></p><p><cust><cust>Test</cust></cust></p>";
            Document doc = NSoupClient.ParseBodyFragment(h);
            string output = doc.Body.Html();
            Assert.AreEqual(h, TextUtil.StripNewLines(output));
        }

        [TestMethod]
        public void parsesBodyFragment()
        {
            string h = "<!-- comment --><p><a href='foo'>One</a></p>";
            Document doc = NSoupClient.ParseBodyFragment(h, "http://example.com");
            Assert.AreEqual("<body><!-- comment --><p><a href=\"foo\">One</a></p></body>", TextUtil.StripNewLines(doc.Body.OuterHtml()));
            Assert.AreEqual("http://example.com/foo", doc.Select("a").First.AbsUrl("href"));
        }

        [TestMethod]
        public void handlesUnknownNamespaceTags()
        {
            // note that the first foo:bar should not really be allowed to be self closing, if parsed in html mode.
            string h = "<foo:bar id='1' /><abc:def id=2>Foo<p>Hello</p></abc:def><foo:bar>There</foo:bar>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<foo:bar id=\"1\" /><abc:def id=\"2\">Foo<p>Hello</p></abc:def><foo:bar>There</foo:bar>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesKnownEmptyBlocks()
        {
            // if known tag, must be defined as self closing to allow as self closing. unkown tags can be self closing.
            string h = "<div id='1' /><div id=2><img /><img></div> <hr /> hr text <hr> hr text two";
            Document doc = NSoupClient.Parse(h);
            Element div1 = doc.GetElementById("1");
            Assert.IsTrue(!div1.Children.IsEmpty); // <div /> is treated as <div>...
            Assert.IsTrue(doc.Select("hr").First.Children.IsEmpty);
            Assert.IsTrue(doc.Select("hr").Last.Children.IsEmpty);
            Assert.IsTrue(doc.Select("img").First.Children.IsEmpty);
            Assert.IsTrue(doc.Select("img").Last.Children.IsEmpty);
        }

        [TestMethod]
        public void handlesSolidusAtAttributeEnd()
        {
            // this test makes sure [<a href=/>link</a>] is parsed as [<a href="/">link</a>], not [<a href="" /><a>link</a>]
            string h = "<a href=/>link</a>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<a href=\"/\">link</a>", doc.Body.Html());
        }

        [TestMethod]
        public void handlesMultiClosingBody()
        {
            string h = "<body><p>Hello</body><p>there</p></body></body></html><p>now";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual(3, doc.Select("p").Count);
            Assert.AreEqual(3, doc.Body.Children.Count);
        }

        [TestMethod]
        public void handlesUnclosedDefinitionLists()
        {
            // NSoupClient used to create a <dl>, but that's not to spec
            string h = "<dt>Foo<dd>Bar<dt>Qux<dd>Zug";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual(0, doc.Select("dl").Count); // no auto dl
            Assert.AreEqual(4, doc.Select("dt, dd").Count);
            Elements dts = doc.Select("dt");
            Assert.AreEqual(2, dts.Count);
            Assert.AreEqual("Zug", dts[1].NextElementSibling.Text());
        }

        [TestMethod]
        public void handlesBlocksInDefinitions()
        {
            // per the spec, dt and dd are inline, but in practise are block
            string h = "<dl><dt><div id=1>Term</div></dt><dd><div id=2>Def</div></dd></dl>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("dt", doc.Select("#1").First.Parent.TagName());
            Assert.AreEqual("dd", doc.Select("#2").First.Parent.TagName());
            Assert.AreEqual("<dl><dt><div id=\"1\">Term</div></dt><dd><div id=\"2\">Def</div></dd></dl>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesFrames()
        {
            string h = "<html><head><script></script><noscript></noscript></head><frameset><frame src=foo></frame><frame src=foo></frameset></html>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<html><head><script></script><noscript></noscript></head><frameset><frame src=\"foo\" /><frame src=\"foo\" /></frameset></html>",
                    TextUtil.StripNewLines(doc.Html()));
            // no body auto vivification
        }

        [TestMethod]
        public void ignoresContentAfterFrameset()
        {
            string h = "<html><head><title>One</title></head><frameset><frame /><frame /></frameset><table></table></html>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<html><head><title>One</title></head><frameset><frame /><frame /></frameset></html>", TextUtil.StripNewLines(doc.Html()));
            // no body, no table. No crash!
        }

        [TestMethod]
        public void handlesJavadocFont()
        {
            string h = "<TD BGCOLOR=\"#EEEEFF\" CLASS=\"NavBarCell1\">    <A HREF=\"deprecated-list.html\"><FONT CLASS=\"NavBarFont1\"><B>Deprecated</B></FONT></A>&nbsp;</TD>";
            Document doc = NSoupClient.Parse(h);
            Element a = doc.Select("a").First;
            Assert.AreEqual("Deprecated", a.Text());
            Assert.AreEqual("font", a.Child(0).TagName());
            Assert.AreEqual("b", a.Child(0).Child(0).TagName());
        }

        [TestMethod]
        public void handlesBaseWithoutHref()
        {
            string h = "<head><base target='_blank'></head><body><a href=/foo>Test</a></body>";
            Document doc = NSoupClient.Parse(h, "http://example.com/");
            Element a = doc.Select("a").First;
            Assert.AreEqual("/foo", a.Attr("href"));
            Assert.AreEqual("http://example.com/foo", a.Attr("abs:href"));
        }

        [TestMethod]
        public void normalisesDocument()
        {
            string h = "<!doctype html>One<html>Two<head>Three<link></head>Four<body>Five </body>Six </html>Seven ";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<!DOCTYPE html><html><head></head><body>OneTwoThree<link />FourFive Six Seven </body></html>",
                    TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void normalisesEmptyDocument()
        {
            Document doc = NSoupClient.Parse("");
            Assert.AreEqual("<html><head></head><body></body></html>", TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void normalisesHeadlessBody()
        {
            Document doc = NSoupClient.Parse("<html><body><span class=\"foo\">bar</span>");
            Assert.AreEqual("<html><head></head><body><span class=\"foo\">bar</span></body></html>",
                    TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void normalisedBodyAfterContent()
        {
            Document doc = NSoupClient.Parse("<font face=Arial><body class=name><div>One</div></body></font>");
            Assert.AreEqual("<html><head></head><body class=\"name\"><font face=\"Arial\"><div>One</div></font></body></html>",
                    TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void findsCharsetInMalformedMeta()
        {
            string h = "<meta http-equiv=Content-Type content=text/html; charset=gb2312>";
            // example cited for reason of html5's <meta charset> element
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("gb2312", doc.Select("meta").Attr("charset"));
        }

        [TestMethod]
        public void testHgroup()
        {
            // NSoupClient used to not allow hroup in h{n}, but that's not in spec, and browsers are OK
            Document doc = NSoupClient.Parse("<h1>Hello <h2>There <hgroup><h1>Another<h2>headline</hgroup> <hgroup><h1>More</h1><p>stuff</p></hgroup>");
            Assert.AreEqual("<h1>Hello </h1><h2>There <hgroup><h1>Another</h1><h2>headline</h2></hgroup> <hgroup><h1>More</h1><p>stuff</p></hgroup></h2>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testRelaxedTags()
        {
            Document doc = NSoupClient.Parse("<abc_def id=1>Hello</abc_def> <abc-def>There</abc-def>");
            Assert.AreEqual("<abc_def id=\"1\">Hello</abc_def> <abc-def>There</abc-def>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testHeaderContents()
        {
            // h* tags (h1 .. h9) in browsers can handle any internal content other than other h*. which is not per any
            // spec, which defines them as containing phrasing content only. so, reality over theory.
            Document doc = NSoupClient.Parse("<h1>Hello <div>There</div> now</h1> <h2>More <h3>Content</h3></h2>");
            Assert.AreEqual("<h1>Hello <div>There</div> now</h1> <h2>More </h2><h3>Content</h3>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testSpanContents()
        {
            // like h1 tags, the spec says SPAN is phrasing only, but browsers and publisher treat span as a block tag
            Document doc = NSoupClient.Parse("<span>Hello <div>there</div> <span>now</span></span>");
            Assert.AreEqual("<span>Hello <div>there</div> <span>now</span></span>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testNoImagesInNoScriptInHead()
        {
            // NSoupClient used to allow, but against spec if parsing with noscript
            Document doc = NSoupClient.Parse("<html><head><noscript><img src='foo'></noscript></head><body><p>Hello</p></body></html>");
            Assert.AreEqual("<html><head><noscript></noscript></head><body><img src=\"foo\" /><p>Hello</p></body></html>", TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void testAFlowContents()
        {
            // html5 has <a> as either phrasing or block
            Document doc = NSoupClient.Parse("<a>Hello <div>there</div> <span>now</span></a>");
            Assert.AreEqual("<a>Hello <div>there</div> <span>now</span></a>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void testFontFlowContents()
        {
            // html5 has no definition of <font>; often used as flow
            Document doc = NSoupClient.Parse("<font>Hello <div>there</div> <span>now</span></font>");
            Assert.AreEqual("<font>Hello <div>there</div> <span>now</span></font>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesMisnestedTagsBI()
        {
            // whatwg: <b><i></b></i>
            string h = "<p>1<b>2<i>3</b>4</i>5</p>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<p>1<b>2<i>3</i></b><i>4</i>5</p>", doc.Body.Html());
            // adoption agency on </b>, reconstruction of formatters on 4.
        }

        [TestMethod]
        public void handlesMisnestedTagsBP()
        {
            //  whatwg: <b><p></b></p>
            string h = "<b>1<p>2</b>3</p>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<b>1</b>\n<p><b>2</b>3</p>", doc.Body.Html());
        }

        [TestMethod]
        public void handlesUnexpectedMarkupInTables()
        {
            // whatwg - tests markers in active formatting (if they didn't work, would get in in table)
            // also tests foster parenting
            string h = "<table><b><tr><td>aaa</td></tr>bbb</table>ccc";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<b></b><b>bbb</b><table><tbody><tr><td>aaa</td></tr></tbody></table><b>ccc</b>", TextUtil.StripNewLines(doc.Body.Html()));
        }

        [TestMethod]
        public void handlesUnclosedFormattingElements()
        {
            // whatwg: formatting elements get collected and applied, but excess elements are thrown away
            string h = "<!DOCTYPE html>\n" +
                    "<p><b class=x><b class=x><b><b class=x><b class=x><b>X\n" +
                    "<p>X\n" +
                    "<p><b><b class=x><b>X\n" +
                    "<p></b></b></b></b></b></b>X";
            Document doc = NSoupClient.Parse(h);
            doc.OutputSettings().IndentAmount(0);
            string want = "<!DOCTYPE html>\n" +
                    "<html>\n" +
                    "<head></head>\n" +
                    "<body>\n" +
                    "<p><b class=\"x\"><b class=\"x\"><b><b class=\"x\"><b class=\"x\"><b>X </b></b></b></b></b></b></p>\n" +
                    "<p><b class=\"x\"><b><b class=\"x\"><b class=\"x\"><b>X </b></b></b></b></b></p>\n" +
                    "<p><b class=\"x\"><b><b class=\"x\"><b class=\"x\"><b><b><b class=\"x\"><b>X </b></b></b></b></b></b></b></b></p>\n" +
                    "<p>X</p>\n" +
                    "</body>\n" +
                    "</html>";
            Assert.AreEqual(want, doc.Html());
        }

        [TestMethod]
        public void reconstructFormattingElements()
        {
            // tests attributes and multi b
            string h = "<p><b class=one>One <i>Two <b>Three</p><p>Hello</p>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<p><b class=\"one\">One <i>Two <b>Three</b></i></b></p>\n<p><b class=\"one\"><i><b>Hello</b></i></b></p>", doc.Body.Html());
        }

        [TestMethod]
        public void reconstructFormattingElementsInTable()
        {
            // tests that tables get formatting markers -- the <b> applies outside the table and does not leak in,
            // and the <i> inside the table and does not leak out.
            string h = "<p><b>One</p> <table><tr><td><p><i>Three<p>Four</i></td></tr></table> <p>Five</p>";
            Document doc = NSoupClient.Parse(h);
            string want = "<p><b>One</b></p>\n" +
                    "<b> \n" +
                    " <table>\n" +
                    "  <tbody>\n" +
                    "   <tr>\n" +
                    "    <td><p><i>Three</i></p><p><i>Four</i></p></td>\n" +
                    "   </tr>\n" +
                    "  </tbody>\n" +
                    " </table> <p>Five</p></b>";
            Assert.AreEqual(want, doc.Body.Html());
        }

        [TestMethod]
        public void commentBeforeHtml()
        {
            string h = "<!-- comment --><!-- comment 2 --><p>One</p>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<!-- comment --><!-- comment 2 --><html><head></head><body><p>One</p></body></html>", TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void emptyTdTag()
        {
            string h = "<table><tr><td>One</td><td id='2' /></tr></table>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual("<td>One</td>\n<td id=\"2\"></td>", doc.Select("tr").First.Html());
        }

        [TestMethod]
        public void handlesSolidusInA()
        {
            // test for bug #66
            string h = "<a class=lp href=/lib/14160711/>link text</a>";
            Document doc = NSoupClient.Parse(h);
            Element a = doc.Select("a").First;
            Assert.AreEqual("link text", a.Text());
            Assert.AreEqual("/lib/14160711/", a.Attr("href"));
        }

        [TestMethod]
        public void handlesSpanInTBody()
        {
            // test for bug 64
            string h = "<table><tbody><span class='1'><tr><td>One</td></tr><tr><td>Two</td></tr></span></tbody></table>";
            Document doc = NSoupClient.Parse(h);
            Assert.AreEqual(doc.Select("span").First.Children.Count, 0); // the span gets closed
            Assert.AreEqual(doc.Select("table").Count, 1); // only one table
        }

        [TestMethod]
        public void handlesUnclosedTitleAtEof()
        {
            Assert.AreEqual("Data", NSoupClient.Parse("<title>Data").Title);
            Assert.AreEqual("Data<", NSoupClient.Parse("<title>Data<").Title);
            Assert.AreEqual("Data</", NSoupClient.Parse("<title>Data</").Title);
            Assert.AreEqual("Data</t", NSoupClient.Parse("<title>Data</t").Title);
            Assert.AreEqual("Data</ti", NSoupClient.Parse("<title>Data</ti").Title);
            Assert.AreEqual("Data", NSoupClient.Parse("<title>Data</title>").Title);
            Assert.AreEqual("Data", NSoupClient.Parse("<title>Data</title >").Title);
        }

        [TestMethod]
        public void handlesUnclosedTitle()
        {
            Document one = NSoupClient.Parse("<title>One <b>Two <b>Three</TITLE><p>Test</p>"); // has title, so <b> is plain text
            Assert.AreEqual("One <b>Two <b>Three", one.Title);
            Assert.AreEqual("Test", one.Select("p").First.Text());

            Document two = NSoupClient.Parse("<title>One<b>Two <p>Test</p>"); // no title, so <b> causes </title> breakout
            Assert.AreEqual("One", two.Title);
            Assert.AreEqual("<b>Two <p>Test</p></b>", two.Body.Html());
        }

        [TestMethod]
        public void handlesUnclosedScriptAtEof()
        {
            Assert.AreEqual("Data", NSoupClient.Parse("<script>Data").Select("script").First.Data);
            Assert.AreEqual("Data<", NSoupClient.Parse("<script>Data<").Select("script").First.Data);
            Assert.AreEqual("Data</sc", NSoupClient.Parse("<script>Data</sc").Select("script").First.Data);
            Assert.AreEqual("Data</-sc", NSoupClient.Parse("<script>Data</-sc").Select("script").First.Data);
            Assert.AreEqual("Data</sc-", NSoupClient.Parse("<script>Data</sc-").Select("script").First.Data);
            Assert.AreEqual("Data</sc--", NSoupClient.Parse("<script>Data</sc--").Select("script").First.Data);
            Assert.AreEqual("Data", NSoupClient.Parse("<script>Data</script>").Select("script").First.Data);
            Assert.AreEqual("Data</script", NSoupClient.Parse("<script>Data</script").Select("script").First.Data);
            Assert.AreEqual("Data", NSoupClient.Parse("<script>Data</script ").Select("script").First.Data);
            Assert.AreEqual("Data", NSoupClient.Parse("<script>Data</script n").Select("script").First.Data);
            Assert.AreEqual("Data", NSoupClient.Parse("<script>Data</script n=").Select("script").First.Data);
            Assert.AreEqual("Data", NSoupClient.Parse("<script>Data</script n=\"").Select("script").First.Data);
            Assert.AreEqual("Data", NSoupClient.Parse("<script>Data</script n=\"p").Select("script").First.Data);
        }

        [TestMethod]
        public void handlesUnclosedRawtextAtEof()
        {
            Assert.AreEqual("Data", NSoupClient.Parse("<style>Data").Select("style").First.Data);
            Assert.AreEqual("Data</st", NSoupClient.Parse("<style>Data</st").Select("style").First.Data);
            Assert.AreEqual("Data", NSoupClient.Parse("<style>Data</style>").Select("style").First.Data);
            Assert.AreEqual("Data</style", NSoupClient.Parse("<style>Data</style").Select("style").First.Data);
            Assert.AreEqual("Data</-style", NSoupClient.Parse("<style>Data</-style").Select("style").First.Data);
            Assert.AreEqual("Data</style-", NSoupClient.Parse("<style>Data</style-").Select("style").First.Data);
            Assert.AreEqual("Data</style--", NSoupClient.Parse("<style>Data</style--").Select("style").First.Data);
        }

        [TestMethod]
        public void noImplicitFormForTextAreas()
        {
            // old NSoupClient parser would create implicit forms for form children like <textarea>, but no more
            Document doc = NSoupClient.Parse("<textarea>One</textarea>");
            Assert.AreEqual("<textarea>One</textarea>", doc.Body.Html());
        }

        [TestMethod]
        public void handlesEscapedScript()
        {
            Document doc = NSoupClient.Parse("<script><!-- one <script>Blah</script> --></script>");
            Assert.AreEqual("<!-- one <script>Blah</script> -->", doc.Select("script").First.Data);
        }

        [TestMethod]
        public void handles0CharacterAsText()
        {
            Document doc = NSoupClient.Parse("0<p>0</p>");
            Assert.AreEqual("0\n<p>0</p>", doc.Body.Html());
        }

        [TestMethod]
        public void handlesNullInData()
        {
            Document doc = NSoupClient.Parse("<p id=\u0000>Blah \u0000</p>");
            Assert.AreEqual("<p id=\"\uFFFD\">Blah \u0000</p>", doc.Body.Html()); // replaced in attr, NOT replaced in data
        }

        [TestMethod]
        public void handlesNullInComments()
        {
            Document doc = NSoupClient.Parse("<body><!-- \u0000 \u0000 -->");
            Assert.AreEqual("<!-- \uFFFD \uFFFD -->", doc.Body.Html());
        }

        [TestMethod]
        public void handlesNewlinesAndWhitespaceInTag()
        {
            Document doc = NSoupClient.Parse("<a \n href=\"one\" \r\n id=\"two\" \f >");
            Assert.AreEqual("<a href=\"one\" id=\"two\"></a>", doc.Body.Html());
        }

        [TestMethod]
        public void handlesWhitespaceInoDocType()
        {
            string html = "<!DOCTYPE html\r\n" +
                    "      PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\"\r\n" +
                    "      \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
            Document doc = NSoupClient.Parse(html);
            Assert.AreEqual("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">", doc.ChildNodes[0].OuterHtml());
        }

        [TestMethod]
        public void tracksErrorsWhenRequested()
        {
            string html = "<p>One</p href='no'><!DOCTYPE html>&arrgh;<font /><br /><foo";
            NSoup.Parse.Parser parser = NSoup.Parse.Parser.HtmlParser().SetTrackErrors(500);
            Document doc = NSoupClient.Parse(html, "http://example.com", parser);

            List<ParseError> errors = parser.GetErrors();
            Assert.AreEqual(5, errors.Count);
            Assert.AreEqual("20: Attributes incorrectly present on end tag", errors[0].ToString());
            Assert.AreEqual("35: Unexpected token [Doctype] when in state [InBody]", errors[1].ToString());
            Assert.AreEqual("36: Invalid character reference: Invalid named referenece 'arrgh'", errors[2].ToString());
            Assert.AreEqual("50: Self closing flag not acknowledged", errors[3].ToString());
            Assert.AreEqual("61: Unexpectedly reached end of file (EOF) in input state [TagName]", errors[4].ToString());
        }

        [TestMethod]
        public void tracksLimitedErrorsWhenRequested()
        {
            string html = "<p>One</p href='no'><!DOCTYPE html>&arrgh;<font /><br /><foo";
            NSoup.Parse.Parser parser = NSoup.Parse.Parser.HtmlParser().SetTrackErrors(3);
            Document doc = parser.ParseInput(html, "http://example.com");

            List<ParseError> errors = parser.GetErrors();
            Assert.AreEqual(3, errors.Count);
            Assert.AreEqual("20: Attributes incorrectly present on end tag", errors[0].ToString());
            Assert.AreEqual("35: Unexpected token [Doctype] when in state [InBody]", errors[1].ToString());
            Assert.AreEqual("36: Invalid character reference: Invalid named referenece 'arrgh'", errors[2].ToString());
        }

        [TestMethod]
        public void noErrorsByDefault()
        {
            string html = "<p>One</p href='no'>&arrgh;<font /><br /><foo";
            NSoup.Parse.Parser parser = NSoup.Parse.Parser.HtmlParser();
            Document doc = NSoupClient.Parse(html, "http://example.com", parser);

            List<ParseError> errors = parser.GetErrors();
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void handlesCommentsInTable()
        {
            string html = "<table><tr><td>text</td><!-- Comment --></tr></table>";
            Document node = NSoupClient.ParseBodyFragment(html);
            Assert.AreEqual("<html><head></head><body><table><tbody><tr><td>text</td><!-- Comment --></tr></tbody></table></body></html>", TextUtil.StripNewLines(node.OuterHtml()));
        }

        [TestMethod]
        public void handlesQuotesInCommentsInScripts()
        {
            string html = "<script>\n" +
                    "  <!--\n" +
                    "    document.write('</scr' + 'ipt>');\n" +
                    "  // -->\n" +
                    "</script>";
            Document node = NSoupClient.ParseBodyFragment(html);
            Assert.AreEqual("<script>\n" +
                    "  <!--\n" +
                    "    document.write('</scr' + 'ipt>');\n" +
                    "  // -->\n" +
                    "</script>", node.Body.Html());
        }

        [TestMethod]
        public void handleNullContextInParseFragment()
        {
            string html = "<ol><li>One</li></ol><p>Two</p>";
            IList<Node> nodes = NSoup.Parse.Parser.ParseFragment(html, null, "http://example.com/");
            Assert.AreEqual(1, nodes.Count); // returns <html> node (not document) -- no context means doc gets created
            Assert.AreEqual("html", nodes[0].NodeName);
            Assert.AreEqual("<html> <head></head> <body> <ol> <li>One</li> </ol> <p>Two</p> </body> </html>", StringUtil.NormaliseWhitespace(nodes[0].OuterHtml()));
        }

        [TestMethod]
        public void doesNotFindShortestMatchingEntity()
        {
            // previous behaviour was to identify a possible entity, then chomp down the string until a match was found.
            // (as defined in html5.) However in practise that lead to spurious matches against the author's intent.
            string html = "One &clubsuite; &clubsuit;";
            Document doc = NSoupClient.Parse(html);
            Assert.AreEqual(StringUtil.NormaliseWhitespace("One &amp;clubsuite; ג™£"), doc.Body.Html());
        }

        [TestMethod]
        public void relaxedBaseEntityMatchAndStrictExtendedMatch()
        {
            // extended entities need a ; at the end to match, base does not
            string html = "&amp &quot &reg &icy &hopf &icy; &hopf;";
            Document doc = NSoupClient.Parse(html);
            doc.OutputSettings().EscapeMode = Entities.EscapeMode.Extended; // modifies output only to clarify test
            Assert.AreEqual(StringUtil.NormaliseWhitespace("&amp; &quot; &reg; &amp;icy &amp;hopf &icy; &hopf;"), doc.Body.Html());
        }
    }
}