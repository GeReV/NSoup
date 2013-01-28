using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup;

namespace Test.Nodes
{

    [TestClass]
    public class EntitiesTest
    {
        public EntitiesTest()
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
        public void escape()
        {
            string text = "Hello &<> Å å π 新 there ¾ ©";
            string escapedAscii = Entities.Escape(text, Encoding.ASCII, Entities.EscapeMode.Base);
            string escapedAsciiFull = Entities.Escape(text, Encoding.ASCII, Entities.EscapeMode.Extended);
            string escapedAsciiXhtml = Entities.Escape(text, Encoding.ASCII, Entities.EscapeMode.Xhtml);
            string escapedUtf = Entities.Escape(text, Encoding.UTF8, Entities.EscapeMode.Base);

            Assert.AreEqual("Hello &amp;&lt;&gt; &Aring; &aring; &#960; &#26032; there &frac34; &copy;", escapedAscii);
            Assert.AreEqual("Hello &amp;&lt;&gt; &angst; &aring; &pi; &#26032; there &frac34; &copy;", escapedAsciiFull);
            Assert.AreEqual("Hello &amp;&lt;&gt; &#197; &#229; &#960; &#26032; there &#190; &#169;", escapedAsciiXhtml);
            Assert.AreEqual("Hello &amp;&lt;&gt; &Aring; &aring; π 新 there &frac34; &copy;", escapedUtf);
            // odd that it's defined as aring in base but angst in full
        }

        [TestMethod]
        public void unescape()
        {
            string text = "Hello &amp;&LT&gt; &reg &angst; &angst &#960; &#960 &#x65B0; there &! &frac34; &copy; &COPY;";
            Assert.AreEqual("Hello &<> ® Å &angst π π 新 there &! ¾ © ©", Entities.Unescape(text));

            Assert.AreEqual("&0987654321; &unknown", Entities.Unescape("&0987654321; &unknown"));
        }

        [TestMethod]
        public void strictUnescape()
        { // for attributes, enforce strict unescaping (must look like &xxx; , not just &xxx)
            string text = "Hello &amp= &amp;";
            Assert.AreEqual("Hello &amp= &", Entities.Unescape(text, true));
            Assert.AreEqual("Hello &= &", Entities.Unescape(text));
            Assert.AreEqual("Hello &= &", Entities.Unescape(text, false));
        }

        [TestMethod]
        public void caseSensitive()
        {
            string unescaped = "Ü ü & &";
            Assert.AreEqual("&Uuml; &uuml; &amp; &amp;", Entities.Escape(unescaped, Encoding.ASCII, Entities.EscapeMode.Extended));

            string escaped = "&Uuml; &uuml; &amp; &AMP";
            Assert.AreEqual("Ü ü & &", Entities.Unescape(escaped));
        }

        [TestMethod]
        public void quoteReplacements()
        {
            string escaped = "&#92; &#36;";
            string unescaped = "\\ $";

            Assert.AreEqual(unescaped, Entities.Unescape(escaped));
        }

        [TestMethod]
        public void letterDigitEntities()
        {
            string html = "<p>&sup1;&sup2;&sup3;&frac14;&frac12;&frac34;</p>";
            Document doc = NSoupClient.Parse(html);
            Element p = doc.Select("p").First;
            Assert.AreEqual("&sup1;&sup2;&sup3;&frac14;&frac12;&frac34;", p.Html());
            Assert.AreEqual("¹²³¼½¾", p.Text());
        }

        [TestMethod]
        public void noSpuriousDecodes()
        {
            string s = "http://www.foo.com?a=1&num_rooms=1&children=0&int=VA&b=2";
            Assert.AreEqual(s, Entities.Unescape(s));
        }
    }
}
