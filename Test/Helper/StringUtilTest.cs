using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup;
using NSoup.Helper;

namespace Test.Helper
{

    [TestClass]
    public class StringUtilTest
    {
        public StringUtilTest()
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
        public void join()
        {
            Assert.AreEqual("", StringUtil.Join(new List<string>() { "" }, " "));
            Assert.AreEqual("one", StringUtil.Join(new List<string>() { "one" }, " "));
            Assert.AreEqual("one two three", StringUtil.Join(new List<string>() { "one", "two", "three" }, " "));
        }

        [TestMethod]
        public void padding()
        {
            Assert.AreEqual("", StringUtil.Padding(0));
            Assert.AreEqual(" ", StringUtil.Padding(1));
            Assert.AreEqual("  ", StringUtil.Padding(2));
            Assert.AreEqual("               ", StringUtil.Padding(15));
        }

        [TestMethod]
        public void isBlank()
        {
            Assert.IsTrue(StringUtil.IsBlank(null));
            Assert.IsTrue(StringUtil.IsBlank(""));
            Assert.IsTrue(StringUtil.IsBlank("      "));
            Assert.IsTrue(StringUtil.IsBlank("   \r\n  "));

            Assert.IsFalse(StringUtil.IsBlank("hello"));
            Assert.IsFalse(StringUtil.IsBlank("   hello   "));
        }

        [TestMethod]
        public void isNumeric()
        {
            Assert.IsFalse(StringUtil.IsNumeric(null));
            Assert.IsFalse(StringUtil.IsNumeric(" "));
            Assert.IsFalse(StringUtil.IsNumeric("123 546"));
            Assert.IsFalse(StringUtil.IsNumeric("hello"));
            Assert.IsFalse(StringUtil.IsNumeric("123.334"));

            Assert.IsTrue(StringUtil.IsNumeric("1"));
            Assert.IsTrue(StringUtil.IsNumeric("1234"));
        }

        [TestMethod]
        public void normaliseWhiteSpace()
        {
            Assert.AreEqual(" ", StringUtil.NormaliseWhitespace("    \r \n \r\n"));
            Assert.AreEqual(" hello there ", StringUtil.NormaliseWhitespace("   hello   \r \n  there    \n"));
            Assert.AreEqual("hello", StringUtil.NormaliseWhitespace("hello"));
            Assert.AreEqual("hello there", StringUtil.NormaliseWhitespace("hello\nthere"));
        }

        [TestMethod]
        public void normaliseWhiteSpaceModified()
        {
            String check1 = "Hello there";
            String check2 = "Hello\nthere";
            String check3 = "Hello  there";

            // does not create new string no mods done
            Assert.IsTrue(check1 == StringUtil.NormaliseWhitespace(check1));
            Assert.IsTrue(check2 != StringUtil.NormaliseWhitespace(check2));
            Assert.IsTrue(check3 != StringUtil.NormaliseWhitespace(check3));
        }
    }
}
