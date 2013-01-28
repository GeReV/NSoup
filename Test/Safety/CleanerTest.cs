using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Safety;
using NSoup;

namespace Test.Safety
{
    /// <summary>
    /// Tests for the cleaner.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class CleanerTest
    {
        public CleanerTest()
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
        public void simpleBehaviourTest()
        {
            string h = "<div><p class=foo><a href='http://evil.com'>Hello <b id=bar>there</b>!</a></div>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.SimpleText);

            Assert.AreEqual("Hello <b>there</b>!", TextUtil.StripNewLines(cleanHtml));
        }

        [TestMethod]
        public void simpleBehaviourTest2()
        {
            string h = "Hello <b>there</b>!";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.SimpleText);

            Assert.AreEqual("Hello <b>there</b>!", TextUtil.StripNewLines(cleanHtml));
        }

        [TestMethod]
        public void basicBehaviourTest()
        {
            string h = "<div><p><a href='javascript:sendAllMoney()'>Dodgy</a> <A HREF='HTTP://nice.com'>Nice</a></p><blockquote>Hello</blockquote>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.Basic);

            /*Assert.AreEqual("<p><a rel=\"nofollow\">Dodgy</a> <a href=\"http://nice.com\" rel=\"nofollow\">Nice</a></p><blockquote>Hello</blockquote>",
                    TextUtil.StripNewLines(cleanHtml));*/
            Assert.AreEqual("<p><a rel=\"nofollow\">Dodgy</a> <a href=\"http://nice.com/\" rel=\"nofollow\">Nice</a></p><blockquote>Hello</blockquote>",
                    TextUtil.StripNewLines(cleanHtml)); // Added forward-slash after nice.com due to limitations with System.Uri adding slash automatically.
        }

        [TestMethod]
        public void basicWithImagesTest()
        {
            string h = "<div><p><img src='http://example.com/' alt=Image></p><p><img src='ftp://ftp.example.com'></p></div>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.BasicWithImages);
            Assert.AreEqual("<p><img src=\"http://example.com/\" alt=\"Image\" /></p><p><img /></p>", TextUtil.StripNewLines(cleanHtml));
        }

        [TestMethod]
        public void testRelaxed()
        {
            string h = "<h1>Head</h1><table><tr><td>One<td>Two</td></tr></table>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.Relaxed);
            Assert.AreEqual("<h1>Head</h1><table><tbody><tr><td>One</td><td>Two</td></tr></tbody></table>", TextUtil.StripNewLines(cleanHtml));
        }

        [TestMethod]
        public void testDropComments()
        {
            string h = "<p>Hello<!-- no --></p>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.Relaxed);
            Assert.AreEqual("<p>Hello</p>", cleanHtml);
        }

        [TestMethod]
        public void testDropXmlProc()
        {
            string h = "<?import namespace=\"xss\"><p>Hello</p>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.Relaxed);
            Assert.AreEqual("<p>Hello</p>", cleanHtml);
        }

        [TestMethod]
        public void testDropScript()
        {
            string h = "<SCRIPT SRC=//ha.ckers.org/.j><SCRIPT>alert(/XSS/.source)</SCRIPT>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.Relaxed);
            Assert.AreEqual("", cleanHtml);
        }

        [TestMethod]
        public void testDropImageScript()
        {
            string h = "<IMG SRC=\"javascript:alert('XSS')\">";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.Relaxed);
            Assert.AreEqual("<img />", cleanHtml);
        }

        [TestMethod]
        public void testCleanJavascriptHref()
        {
            string h = "<A HREF=\"javascript:document.location='http://www.google.com/'\">XSS</A>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.Relaxed);
            Assert.AreEqual("<a>XSS</a>", cleanHtml);
        }

        [TestMethod]
        public void testDropsUnknownTags()
        {
            string h = "<p><custom foo=true>Test</custom></p>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.Relaxed);
            Assert.AreEqual("<p>Test</p>", cleanHtml);
        }

        [TestMethod]
        public void testHandlesEmptyAttributes()
        {
            string h = "<img alt=\"\" src= unknown=''>";
            string cleanHtml = NSoup.NSoupClient.Clean(h, Whitelist.BasicWithImages);
            Assert.AreEqual("<img alt=\"\" />", cleanHtml);
        }

        [TestMethod]
        public void testIsValid()
        {
            string ok = "<p>Test <b><a href='http://example.com/'>OK</a></b></p>";
            string nok1 = "<p><script></script>Not <b>OK</b></p>";
            string nok2 = "<p align=right>Test Not <b>OK</b></p>";
            Assert.IsTrue(NSoup.NSoupClient.IsValid(ok, Whitelist.Basic));
            Assert.IsFalse(NSoup.NSoupClient.IsValid(nok1, Whitelist.Basic));
            Assert.IsFalse(NSoup.NSoupClient.IsValid(nok2, Whitelist.Basic));
        }

        [TestMethod]
        public void resolvesRelativeLinks()
        {
            String html = "<a href='/foo'>Link</a><img src='/bar'>";
            String clean = NSoupClient.Clean(html, "http://example.com/", Whitelist.BasicWithImages);
            Assert.AreEqual("<a href=\"http://example.com/foo\" rel=\"nofollow\">Link</a>\n<img src=\"http://example.com/bar\" />", clean);
        }

        [TestMethod]
        public void preservesRelatedLinksIfConfigured()
        {
            string html = "<a href='/foo'>Link</a><img src='/bar'> <img src='javascript:alert()'>";
            string clean = NSoupClient.Clean(html, "http://example.com/", Whitelist.BasicWithImages.PreserveRelativeLinks(true));
            Assert.AreEqual("<a href=\"/foo\" rel=\"nofollow\">Link</a>\n<img src=\"/bar\" /> \n<img />", clean);
        }

        [TestMethod]
        public void dropsUnresolvableRelativeLinks()
        {
            string html = "<a href='/foo'>Link</a>";
            string clean = NSoup.NSoupClient.Clean(html, Whitelist.Basic);
            Assert.AreEqual("<a rel=\"nofollow\">Link</a>", clean);
        }

        [TestMethod]
        public void handlesCustomProtocols()
        {
            String html = "<img src='cid:12345' /> <img src='data:gzzt' />";
            String dropped = NSoupClient.Clean(html, Whitelist.BasicWithImages);
            Assert.AreEqual("<img /> \n<img />", dropped);

            String preserved = NSoupClient.Clean(html, Whitelist.BasicWithImages.AddProtocols("img", "src", "cid", "data"));
            Assert.AreEqual("<img src=\"cid:12345\" /> \n<img src=\"data:gzzt\" />", preserved);
        }

        [TestMethod]
        public void handlesAllPseudoTag()
        {
            String html = "<p class='foo' src='bar'><a class='qux'>link</a></p>";
            Whitelist whitelist = new Whitelist()
                    .AddAttributes(":all", "class")
                    .AddAttributes("p", "style")
                    .AddTags("p", "a");

            String clean = NSoupClient.Clean(html, whitelist);
            Assert.AreEqual("<p class=\"foo\"><a class=\"qux\">link</a></p>", clean);
        }

        [TestMethod]
        public void addsTagOnAttributesIfNotSet()
        {
            String html = "<p class='foo' src='bar'>One</p>";
            Whitelist whitelist = new Whitelist()
                .AddAttributes("p", "class");
            // ^^ whitelist does not have explicit tag add for p, inferred from add attributes.
            String clean = NSoupClient.Clean(html, whitelist);
            Assert.AreEqual("<p class=\"foo\">One</p>", clean);
        }

        [TestMethod]
        public void supplyOutputSettings()
        {
            // test that one can override the default document output settings
            OutputSettings os = new OutputSettings();
            os.PrettyPrint(false);
            os.EscapeMode = Entities.EscapeMode.Extended;

            string html = "<div><p>&bernou;</p></div>";
            string customOut = NSoupClient.Clean(html, "http://foo.com/", Whitelist.Relaxed, os);
            string defaultOut = NSoupClient.Clean(html, "http://foo.com/", Whitelist.Relaxed);
            Assert.AreNotSame(defaultOut, customOut);

            Assert.AreEqual("<div><p>&bernou;</p></div>", customOut);
            Assert.AreEqual("<div>\n" +
                " <p>ג„¬</p>\n" +
                "</div>", defaultOut);

            os.Encoding = Encoding.ASCII;
            os.EscapeMode = Entities.EscapeMode.Base;
            String customOut2 = NSoupClient.Clean(html, "http://foo.com/", Whitelist.Relaxed, os);
            Assert.AreEqual("<div><p>&#8492;</p></div>", customOut2);
        }

        [TestMethod]
        public void handlesFramesets()
        {
            String dirty = "<html><head><script></script><noscript></noscript></head><frameset><frame src=\"foo\" /><frame src=\"foo\" /></frameset></html>";
            String clean = NSoupClient.Clean(dirty, Whitelist.Basic);
            Assert.AreEqual("", clean); // nothing good can come out of that

            Document dirtyDoc = NSoupClient.Parse(dirty);
            Document cleanDoc = new Cleaner(Whitelist.Basic).Clean(dirtyDoc);
            Assert.IsFalse(cleanDoc == null);
            Assert.AreEqual(0, cleanDoc.Body.ChildNodes.Count);
        }

        [TestMethod]
        public void cleansInternationalText()
        {
            Assert.AreEqual("׀¿ׁ€׀¸׀²׀µׁ‚", NSoupClient.Clean("׀¿ׁ€׀¸׀²׀µׁ‚", Whitelist.None));
        }
    }
}