using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Parse;
using NSoup.Nodes;
using NSoup;
using System.IO;

namespace Test.Parser
{

    /**
     * Tests XmlTreeBuilder.
     *
     * @author Jonathan Hedley
     */
    [TestClass]
    public class XmlTreeBuilderTest
    {
        [TestMethod]
        public void testSimpleXmlParse()
        {
            string xml = "<doc id=2 href='/bar'>Foo <br /><link>One</link><link>Two</link></doc>";
            XmlTreeBuilder tb = new XmlTreeBuilder();
            Document doc = tb.Parse(xml, "http://foo.com/");
            Assert.AreEqual("<doc id=\"2\" href=\"/bar\">Foo <br /><link>One</link><link>Two</link></doc>",
                    TextUtil.StripNewLines(doc.Html()));
            Assert.AreEqual(doc.GetElementById("2").AbsUrl("href"), "http://foo.com/bar");
        }

        [TestMethod]
        public void testPopToClose()
        {
            // test: </val> closes Two, </bar> ignored
            string xml = "<doc><val>One<val>Two</val></bar>Three</doc>";
            XmlTreeBuilder tb = new XmlTreeBuilder();
            Document doc = tb.Parse(xml, "http://foo.com/");
            Assert.AreEqual("<doc><val>One<val>Two</val>Three</val></doc>",
                    TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void testCommentAndDocType()
        {
            string xml = "<!DOCTYPE html><!-- a comment -->One <qux />Two";
            XmlTreeBuilder tb = new XmlTreeBuilder();
            Document doc = tb.Parse(xml, "http://foo.com/");
            Assert.AreEqual("<!DOCTYPE html><!-- a comment -->One <qux />Two",
                    TextUtil.StripNewLines(doc.Html()));
        }

        [TestMethod]
        public void testSupplyParserToJsoupClass()
        {
            String xml = "<doc><val>One<val>Two</val></bar>Three</doc>";
            Document doc = NSoupClient.Parse(xml, "http://foo.com/", NSoup.Parse.Parser.XmlParser());
            Assert.AreEqual("<doc><val>One<val>Two</val>Three</val></doc>",
                    TextUtil.StripNewLines(doc.Html()));
        }

        [Ignore]
        [TestMethod]
        public void testSupplyParserToConnection()
        {
            String xmlUrl = "http://direct.infohound.net/tools/jsoup-xml-test.xml";

            // parse with both xml and html parser, ensure different
            Document xmlDoc = NSoupClient.Connect(xmlUrl).Parser(NSoup.Parse.Parser.XmlParser()).Get();
            Document htmlDoc = NSoupClient.Connect(xmlUrl).Get();

            Assert.AreEqual("<doc><val>One<val>Two</val>Three</val></doc>",
                    TextUtil.StripNewLines(xmlDoc.Html()));
            Assert.AreNotSame(htmlDoc, xmlDoc);
            Assert.AreEqual(1, htmlDoc.Select("head").Count); // html parser normalises
            Assert.AreEqual(0, xmlDoc.Select("head").Count); // xml parser does not
        }

        [TestMethod]
        public void testSupplyParserToDataStream() {
            using (Stream input = getFile("Test.htmltests.xml-test.xml"))
            {
                Document doc = NSoupClient.Parse(input, null, "http://foo.com", NSoup.Parse.Parser.XmlParser());
                Assert.AreEqual("<doc><val>One<val>Two</val>Three</val></doc>",
                        TextUtil.StripNewLines(doc.Html()));
            }
        }

        [TestMethod]
        public void testDoesNotForceSelfClosingKnownTags()
        {
            // html will force "<br>one</br>" to "<br />One<br />". XML should be stay "<br>one</br> -- don't recognise tag.
            Document htmlDoc = NSoupClient.Parse("<br>one</br>");
            Assert.AreEqual("<br />one\n<br />", htmlDoc.Body.Html());

            Document xmlDoc = NSoupClient.Parse("<br>one</br>", "", NSoup.Parse.Parser.XmlParser());
            Assert.AreEqual("<br>one</br>", xmlDoc.Html());
        }

        Stream getFile(string resourceName)
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}