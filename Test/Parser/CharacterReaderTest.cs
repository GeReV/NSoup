using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Nodes;
using NSoup.Select;
using NSoup;
using NSoup.Parse;

namespace Test.Parser
{
    /// <summary>
    /// Test suite for character reader.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class CharacterReaderTest
    {
        public CharacterReaderTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the Current test run.
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
        public void consume()
        {
            CharacterReader r = new CharacterReader("one");
            Assert.AreEqual(0, r.Position);
            Assert.AreEqual('o', r.Current());
            Assert.AreEqual('o', r.Consume());
            Assert.AreEqual(1, r.Position);
            Assert.AreEqual('n', r.Current());
            Assert.AreEqual(1, r.Position);
            Assert.AreEqual('n', r.Consume());
            Assert.AreEqual('e', r.Consume());
            Assert.IsTrue(r.IsEmpty());
            Assert.AreEqual(CharacterReader.EOF, r.Consume());
            Assert.IsTrue(r.IsEmpty());
            Assert.AreEqual(CharacterReader.EOF, r.Consume());
        }

        [TestMethod]
        public void unconsume()
        {
            CharacterReader r = new CharacterReader("one");
            Assert.AreEqual('o', r.Consume());
            Assert.AreEqual('n', r.Current());
            r.Unconsume();
            Assert.AreEqual('o', r.Current());

            Assert.AreEqual('o', r.Consume());
            Assert.AreEqual('n', r.Consume());
            Assert.AreEqual('e', r.Consume());
            Assert.IsTrue(r.IsEmpty());
            r.Unconsume();
            Assert.IsFalse(r.IsEmpty());
            Assert.AreEqual('e', r.Current());
            Assert.AreEqual('e', r.Consume());
            Assert.IsTrue(r.IsEmpty());

            Assert.AreEqual(CharacterReader.EOF, r.Consume());
            r.Unconsume();
            Assert.IsTrue(r.IsEmpty());
            Assert.AreEqual(CharacterReader.EOF, r.Current());
        }

        [TestMethod]
        public void mark()
        {
            CharacterReader r = new CharacterReader("one");
            r.Consume();
            r.Mark();
            Assert.AreEqual('n', r.Consume());
            Assert.AreEqual('e', r.Consume());
            Assert.IsTrue(r.IsEmpty());
            r.RewindToMark();
            Assert.AreEqual('n', r.Consume());
        }

        [TestMethod]
        public void consumeToEnd()
        {
            string input = "one two three";
            CharacterReader r = new CharacterReader(input);
            String toEnd = r.ConsumeToEnd();
            Assert.AreEqual(input, toEnd);
            Assert.IsTrue(r.IsEmpty());
        }

        [TestMethod]
        public void nextIndexOfChar()
        {
            string input = "blah blah";
            CharacterReader r = new CharacterReader(input);

            Assert.AreEqual(-1, r.NextIndexOf('x'));
            Assert.AreEqual(3, r.NextIndexOf('h'));
            String pull = r.ConsumeTo('h');
            Assert.AreEqual("bla", pull);
            r.Consume();
            Assert.AreEqual(2, r.NextIndexOf('l'));
            Assert.AreEqual(" blah", r.ConsumeToEnd());
            Assert.AreEqual(-1, r.NextIndexOf('x'));
        }

        [TestMethod]
        public void nextIndexOfString()
        {
            string input = "One Two something Two Three Four";
            CharacterReader r = new CharacterReader(input);

            Assert.AreEqual(-1, r.NextIndexOf("Foo"));
            Assert.AreEqual(4, r.NextIndexOf("Two"));
            Assert.AreEqual("One Two ", r.ConsumeTo("something"));
            Assert.AreEqual(10, r.NextIndexOf("Two"));
            Assert.AreEqual("something Two Three Four", r.ConsumeToEnd());
            Assert.AreEqual(-1, r.NextIndexOf("Two"));
        }

        [TestMethod]
        public void consumeToChar()
        {
            CharacterReader r = new CharacterReader("One Two Three");
            Assert.AreEqual("One ", r.ConsumeTo('T'));
            Assert.AreEqual("", r.ConsumeTo('T')); // on Two
            Assert.AreEqual('T', r.Consume());
            Assert.AreEqual("wo ", r.ConsumeTo('T'));
            Assert.AreEqual('T', r.Consume());
            Assert.AreEqual("hree", r.ConsumeTo('T')); // consume to end
        }

        [TestMethod]
        public void consumeToString()
        {
            CharacterReader r = new CharacterReader("One Two Two Four");
            Assert.AreEqual("One ", r.ConsumeTo("Two"));
            Assert.AreEqual('T', r.Consume());
            Assert.AreEqual("wo ", r.ConsumeTo("Two"));
            Assert.AreEqual('T', r.Consume());
            Assert.AreEqual("wo Four", r.ConsumeTo("Qux"));
        }

        [TestMethod]
        public void advance()
        {
            CharacterReader r = new CharacterReader("One Two Three");
            Assert.AreEqual('O', r.Consume());
            r.Advance();
            Assert.AreEqual('e', r.Consume());
        }

        [TestMethod]
        public void consumeToAny()
        {
            CharacterReader r = new CharacterReader("One &bar; qux");
            Assert.AreEqual("One ", r.ConsumeToAny('&', ';'));
            Assert.IsTrue(r.Matches('&'));
            Assert.IsTrue(r.Matches("&bar;"));
            Assert.AreEqual('&', r.Consume());
            Assert.AreEqual("bar", r.ConsumeToAny('&', ';'));
            Assert.AreEqual(';', r.Consume());
            Assert.AreEqual(" qux", r.ConsumeToAny('&', ';'));
        }

        [TestMethod]
        public void consumeLetterSequence()
        {
            CharacterReader r = new CharacterReader("One &bar; qux");
            Assert.AreEqual("One", r.ConsumeLetterSequence());
            Assert.AreEqual(" &", r.ConsumeTo("bar;"));
            Assert.AreEqual("bar", r.ConsumeLetterSequence());
            Assert.AreEqual("; qux", r.ConsumeToEnd());
        }

        [TestMethod]
        public void consumeLetterThenDigitSequence()
        {
            CharacterReader r = new CharacterReader("One12 Two &bar; qux");
            Assert.AreEqual("One12", r.ConsumeLetterThenDigitSequence());
            Assert.AreEqual(' ', r.Consume());
            Assert.AreEqual("Two", r.ConsumeLetterThenDigitSequence());
            Assert.AreEqual(" &bar; qux", r.ConsumeToEnd());
        }

        [TestMethod]
        public void matches()
        {
            CharacterReader r = new CharacterReader("One Two Three");
            Assert.IsTrue(r.Matches('O'));
            Assert.IsTrue(r.Matches("One Two Three"));
            Assert.IsTrue(r.Matches("One"));
            Assert.IsFalse(r.Matches("one"));
            Assert.AreEqual('O', r.Consume());
            Assert.IsFalse(r.Matches("One"));
            Assert.IsTrue(r.Matches("ne Two Three"));
            Assert.IsFalse(r.Matches("ne Two Three Four"));
            Assert.AreEqual("ne Two Three", r.ConsumeToEnd());
            Assert.IsFalse(r.Matches("ne"));
        }

        [TestMethod]
        public void matchesIgnoreCase()
        {
            CharacterReader r = new CharacterReader("One Two Three");
            Assert.IsTrue(r.MatchesIgnoreCase("O"));
            Assert.IsTrue(r.MatchesIgnoreCase("o"));
            Assert.IsTrue(r.Matches('O'));
            Assert.IsFalse(r.Matches('o'));
            Assert.IsTrue(r.MatchesIgnoreCase("One Two Three"));
            Assert.IsTrue(r.MatchesIgnoreCase("ONE two THREE"));
            Assert.IsTrue(r.MatchesIgnoreCase("One"));
            Assert.IsTrue(r.MatchesIgnoreCase("one"));
            Assert.AreEqual('O', r.Consume());
            Assert.IsFalse(r.MatchesIgnoreCase("One"));
            Assert.IsTrue(r.MatchesIgnoreCase("NE Two Three"));
            Assert.IsFalse(r.MatchesIgnoreCase("ne Two Three Four"));
            Assert.AreEqual("ne Two Three", r.ConsumeToEnd());
            Assert.IsFalse(r.MatchesIgnoreCase("ne"));
        }

        [TestMethod]
        public void containsIgnoreCase()
        {
            CharacterReader r = new CharacterReader("One TWO three");
            Assert.IsTrue(r.ContainsIgnoreCase("two"));
            Assert.IsTrue(r.ContainsIgnoreCase("three"));
            // weird one: does not find one, because it scans for consistent case only
            Assert.IsFalse(r.ContainsIgnoreCase("one"));
        }

        [TestMethod]
        public void matchesAny()
        {
            char[] scan = { ' ', '\n', '\t' };
            CharacterReader r = new CharacterReader("One\nTwo\tThree");
            Assert.IsFalse(r.MatchesAny(scan));
            Assert.AreEqual("One", r.ConsumeToAny(scan));
            Assert.IsTrue(r.MatchesAny(scan));
            Assert.AreEqual('\n', r.Consume());
            Assert.IsFalse(r.MatchesAny(scan));
        }

    }
}