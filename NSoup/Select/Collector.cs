using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Select
{
    /// <summary>
    /// Collects a list of elements that match the supplied criteria.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Collector
    {

        /// <summary>
        /// Build a list of elements, by visiting root and every descendant of root, and testing it against the evaluator.
        /// </summary>
        /// <param name="eval">Evaluator to test elements against</param>
        /// <param name="root">root of tree to descend</param>
        /// <returns>list of matches; empty if none</returns>
        public static Elements Collect(Evaluator eval, Element root)
        {
            Elements elements = new Elements();
            AccumulateMatches(eval, elements, root);
            return elements;
        }

        private static void AccumulateMatches(Evaluator eval, IList<Element> elements, Element element)
        {
            if (eval.Matches(element))
            {
                elements.Add(element);
            }
            foreach (Element child in element.Children)
            {
                AccumulateMatches(eval, elements, child);
            }
        }
    }
}
