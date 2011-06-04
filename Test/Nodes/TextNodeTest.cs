using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;

namespace Test.Nodes
{
    /// <summary>
    /// Test TextNodes
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class TextNodeTest
    {
        public TextNodeTest()
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
        public void testBlank()
        {
            TextNode one = new TextNode("", "");
            TextNode two = new TextNode("     ", "");
            TextNode three = new TextNode("  \n\n   ", "");
            TextNode four = new TextNode("Hello", "");
            TextNode five = new TextNode("  \nHello ", "");

            Assert.IsTrue(one.IsBlank);
            Assert.IsTrue(two.IsBlank);
            Assert.IsTrue(three.IsBlank);
            Assert.IsFalse(four.IsBlank);
            Assert.IsFalse(five.IsBlank);
        }

        [TestMethod]
        public void testTextBean()
        {
            Document doc = NSoup.NSoupClient.Parse("<p>One <span>two &amp;</span> three &amp;</p>");
            Element p = doc.Select("p").First;

            Element span = doc.Select("span").First;
            Assert.AreEqual("two &", span.Text());
            TextNode spanText = (TextNode)span.ChildNodes[0];
            Assert.AreEqual("two &", spanText.Text());

            TextNode tn = (TextNode)p.ChildNodes[2];
            Assert.AreEqual(" three &", tn.Text());

            tn.Text(" POW!");
            Assert.AreEqual("One <span>two &amp;</span> POW!", TextUtil.StripNewLines(p.Html()));

            tn.Attr("text", "kablam &");
            Assert.AreEqual("kablam &", tn.Text());
            Assert.AreEqual("One <span>two &amp;</span>kablam &amp;", TextUtil.StripNewLines(p.Html()));
        }
    }
}
