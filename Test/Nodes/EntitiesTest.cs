using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;

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
            string text = "Hello &<> Å π 新 there";
            string escapedAscii = Entities.Escape(text, Encoding.ASCII, Entities.EscapeMode.Base);
            string escapedAsciiFull = Entities.Escape(text, Encoding.ASCII, Entities.EscapeMode.Extended);
            string escapedUtf = Entities.Escape(text, Encoding.UTF8, Entities.EscapeMode.Base);

            Assert.AreEqual("Hello &amp;&lt;&gt; &aring; &#960; &#26032; there", escapedAscii);
            Assert.AreEqual("Hello &amp;&lt;&gt; &angst; &pi; &#26032; there", escapedAsciiFull);
            Assert.AreEqual("Hello &amp;&lt;&gt; &aring; π 新 there", escapedUtf);
            // odd that it's defined as aring in base but angst in full
        }

        [TestMethod]
        public void unescape()
        {
            string text = "Hello &amp;&LT&gt; &ANGST &#960; &#960 &#x65B0; there &!";
            Assert.AreEqual("Hello &<> Å π π 新 there &!", Entities.Unescape(text));

            Assert.AreEqual("&0987654321; &unknown", Entities.Unescape("&0987654321; &unknown"));
        }
    }
}
