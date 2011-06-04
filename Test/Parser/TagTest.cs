using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Parse;

namespace Test.Parser
{
    /// <summary>
    /// Tag tests.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class TagTest
    {
        public TagTest()
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
        public void isCaseInsensitive()
        {
            Tag p1 = Tag.ValueOf("P");
            Tag p2 = Tag.ValueOf("p");
            Assert.AreEqual(p1, p2);
        }

        [TestMethod]
        public void trims()
        {
            Tag p1 = Tag.ValueOf("p");
            Tag p2 = Tag.ValueOf(" p ");
            Assert.AreEqual(p1, p2);
        }

        [TestMethod]
        public void equality()
        {
            Tag p1 = Tag.ValueOf("p");
            Tag p2 = Tag.ValueOf("p");
            Assert.IsTrue(p1.Equals(p2));
            Assert.IsTrue(p1 == p2);
        }

        [TestMethod]
        public void divSemantics()
        {
            Tag div = Tag.ValueOf("div");
            Tag p = Tag.ValueOf("p");

            Assert.IsTrue(div.CanContain(div));
            Assert.IsTrue(div.CanContain(p));
        }

        [TestMethod]
        public void pSemantics()
        {
            Tag div = Tag.ValueOf("div");
            Tag p = Tag.ValueOf("p");
            Tag img = Tag.ValueOf("img");
            Tag span = Tag.ValueOf("span");

            Assert.IsTrue(p.CanContain(img));
            Assert.IsTrue(p.CanContain(span));
            Assert.IsFalse(p.CanContain(div));
            Assert.IsFalse(p.CanContain(p));
        }

        [TestMethod]
        public void spanSemantics()
        {
            Tag span = Tag.ValueOf("span");
            Tag p = Tag.ValueOf("p");
            Tag div = Tag.ValueOf("div");

            Assert.IsTrue(span.CanContain(span));
            Assert.IsTrue(span.CanContain(p));
            Assert.IsTrue(span.CanContain(div));
        }

        [TestMethod]
        public void imgSemantics()
        {
            Tag img = Tag.ValueOf("img");
            Tag p = Tag.ValueOf("p");

            Assert.IsFalse(img.CanContain(img));
            Assert.IsFalse(img.CanContain(p));
        }

        [TestMethod]
        public void defaultSemantics()
        {
            Tag foo = Tag.ValueOf("foo"); // not defined
            Tag foo2 = Tag.ValueOf("FOO");
            Tag div = Tag.ValueOf("div");

            Assert.AreEqual(foo, foo2);
            Assert.IsTrue(foo.CanContain(foo));
            Assert.IsTrue(foo.CanContain(div));
            Assert.IsTrue(div.CanContain(foo));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ValueOfChecksNotNull()
        {
            Tag.ValueOf(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ValueOfChecksNotEmpty()
        {
            Tag.ValueOf(" ");
        }
    }
}
