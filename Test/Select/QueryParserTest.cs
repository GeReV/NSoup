using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSoup.Select;

namespace Test.Select
{
    /// <summary>
    /// Tests for the Selector Query Parser.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    [TestClass]
    public class QueryParserTest
    {
        [TestMethod]
        public void testOrGetsCorrectPrecedence()
        {
            // tests that a selector "a b, c d, e f" evals to (a AND b) OR (c AND d) OR (e AND f)"
            // top level or, three child ands
            Evaluator eval = QueryParser.Parse("a b, c d, e f");
            Assert.IsTrue(eval is CombiningEvaluator.Or);
            CombiningEvaluator.Or or = (CombiningEvaluator.Or)eval;
            Assert.AreEqual(3, or.Evaluators.Count);
            foreach (Evaluator innerEval in or.Evaluators)
            {
                Assert.IsTrue(innerEval is CombiningEvaluator.And);
                CombiningEvaluator.And and = (CombiningEvaluator.And)innerEval;
                Assert.AreEqual(2, and.Evaluators.Count);
                Assert.IsTrue(and.Evaluators[0] is Evaluator.Tag);
                Assert.IsTrue(and.Evaluators[1] is StructuralEvaluator.Parent);
            }
        }

        [TestMethod]
        public void testParsesMultiCorrectly()
        {
            Evaluator eval = QueryParser.Parse(".foo > ol, ol > li + li");
            Assert.IsTrue(eval is CombiningEvaluator.Or);
            CombiningEvaluator.Or or = (CombiningEvaluator.Or)eval;
            Assert.AreEqual(2, or.Evaluators.Count);

            CombiningEvaluator.And andLeft = (CombiningEvaluator.And)or.Evaluators[0];
            CombiningEvaluator.And andRight = (CombiningEvaluator.And)or.Evaluators[1];

            Assert.AreEqual("ol :ImmediateParent.foo", andLeft.ToString());
            Assert.AreEqual(2, andLeft.Evaluators.Count);
            Assert.AreEqual("li :prevli :ImmediateParentol", andRight.ToString());
            Assert.AreEqual(2, andLeft.Evaluators.Count);
        }
    }
}