using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Parse;
using NSoup.Nodes;

namespace Test.Parser
{
    /// <summary>
    /// Token queue tests.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class TokenQueueTest
    {
        public TokenQueueTest()
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
        public void chompBalanced()
        {
            TokenQueue tq = new TokenQueue(":contains(one (two) three) four");
            string pre = tq.ConsumeTo("(");
            string guts = tq.ChompBalanced('(', ')');
            string remainder = tq.Remainder();

            Assert.AreEqual(":contains", pre);
            Assert.AreEqual("one (two) three", guts);
            Assert.AreEqual(" four", remainder);
        }

        [TestMethod]
        public void chompEscapedBalanced()
        {
            TokenQueue tq = new TokenQueue(":contains(one (two) \\( \\) \\) three) four");
            string pre = tq.ConsumeTo("(");
            string guts = tq.ChompBalanced('(', ')');
            string remainder = tq.Remainder();

            Assert.AreEqual(":contains", pre);
            Assert.AreEqual("one (two) \\( \\) \\) three", guts);
            Assert.AreEqual("one (two) ( ) ) three", TokenQueue.Unescape(guts));
            Assert.AreEqual(" four", remainder);
        }

        [TestMethod]
        public void chompBalancedMatchesAsMuchAsPossible()
        {
            TokenQueue tq = new TokenQueue("unbalanced(something(or another");
            tq.ConsumeTo("(");
            string match = tq.ChompBalanced('(', ')');
            Assert.AreEqual("something(or another", match);
        }

        [TestMethod]
        public void unescape()
        {
            Assert.AreEqual("one ( ) \\", TokenQueue.Unescape("one \\( \\) \\\\"));
        }

        [TestMethod]
        public void chompToIgnoreCase()
        {
            string t = "<textarea>one < two </TEXTarea>";
            TokenQueue tq = new TokenQueue(t);
            string data = tq.ChompToIgnoreCase("</textarea");
            Assert.AreEqual("<textarea>one < two ", data);

            tq = new TokenQueue("<textarea> one two < three </oops>");
            data = tq.ChompToIgnoreCase("</textarea");
            Assert.AreEqual("<textarea> one two < three </oops>", data);
        }

        [TestMethod]
        public void addFirst()
        {
            TokenQueue tq = new TokenQueue("One Two");
            tq.ConsumeWord();
            tq.AddFirst("Three");
            Assert.AreEqual("Three Two", tq.Remainder());
        }
    }
}
