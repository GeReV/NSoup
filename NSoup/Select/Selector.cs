﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;
using NSoup.Parse;

namespace NSoup.Select
{
    /// <summary>
    /// CSS-like element selector, that finds elements matching a query.
    /// </summary>
    /// <seealso cref="Element.Select(string)"/>
    /// <remarks>
    ///  <h2>Selector syntax</h2> 
    ///  A selector is a chain of simple selectors, seperated by combinators. Selectors are case insensitive (including against
    ///  elements, attributes, and attribute values).
    ///  The universal selector (*) is implicit when no element selector is supplied (i.e. {@code *.header} and {@code .header}
    ///  is equivalent).
    ///  
    /// <table>
    /// <tr><th>Pattern</th><th>Matches</th><th>Example</th></tr>
    /// <tr><td><code>*</code></td><td>any element</td><td><code>*</code></td></tr>
    /// <tr><td><code>tag</code></td><td>elements with the given tag name</td><td><code>div</code></td></tr>
    /// <tr><td><code>ns|E</code></td><td>elements of type E in the namespace <i>ns</i></td><td><code>fb|name</code> finds <code>&lt;fb:name></code> elements</td></tr>
    /// <tr><td><code>#id</code></td><td>elements with attribute ID of "id"</td><td><code>div#wrap</code>, <code>#logo</code></td></tr>
    /// <tr><td><code>.class</code></td><td>elements with a class name of "class"</td><td><code>div.left</code>, <code>.result</code></td></tr>
    /// <tr><td><code>[attr]</code></td><td>elements with an attribute named "attr" (with any value)</td><td><code>a[href]</code>, <code>[title]</code></td></tr>
    /// <tr><td><code>[^attrPrefix]</code></td><td>elements with an attribute name starting with "attrPrefix". Use to find elements with HTML5 datasets</td><td><code>[^data-]</code>, <code>div[^data-]</code></td></tr>
    /// <tr><td><code>[attr=val]</code></td><td>elements with an attribute named "attr", and value equal to "val"</td><td><code>img[width=500]</code>, <code>a[rel=nofollow]</code></td></tr>
    /// <tr><td><code>[attr^=valPrefix]</code></td><td>elements with an attribute named "attr", and value starting with "valPrefix"</td><td><code>a[href^=http:]</code></code></td></tr>
    /// <tr><td><code>[attr$=valSuffix]</code></td><td>elements with an attribute named "attr", and value ending with "valSuffix"</td><td><code>img[src$=.png]</code></td></tr>
    /// <tr><td><code>[attr*=valContaining]</code></td><td>elements with an attribute named "attr", and value containing "valContaining"</td><td><code>a[href*=/search/]</code></td></tr>
    /// <tr><td><code>[attr~=<em>regex</em>]</code></td><td>elements with an attribute named "attr", and value matching the regular expression</td><td><code>img[src~=(?i)\\.(png|jpe?g)]</code></td></tr>
    /// <tr><td></td><td>The above may be combined in any order</td><td><code>div.header[title]</code></td></tr>
    /// <tr><td><td colspan="3"><h3>Combinators</h3></td></tr>
    /// <tr><td><code>E F</code></td><td>an F element descended from an E element</td><td><code>div a</code>, <code>.logo h1</code></td></tr>
    /// <tr><td><code>E > F</code></td><td>an F direct child of E</td><td><code>ol > li</code></td></tr>
    /// <tr><td><code>E + F</code></td><td>an F element immediately preceded by sibling E</td><td><code>li + li</code>, <code>div.head + div</code></td></tr>
    /// <tr><td><code>E ~ F</code></td><td>an F element preceded by sibling E</td><td><code>h1 ~ p</code></td></tr>
    /// <tr><td><code>E, F, G</code></td><td>all matching elements E, F, or G</td><td><code>a[href], div, h3</code></td></tr>
    /// <tr><td><td colspan="3"><h3>Pseudo selectors</h3></td></tr>
    /// <tr><td><code>:lt(<em>n</em>)</code></td><td>elements whose sibling index is less than <em>n</em></td><td><code>td:lt(3)</code> finds the first 2 cells of each row</td></tr>
    /// <tr><td><code>:gt(<em>n</em>)</code></td><td>elements whose sibling index is greater than <em>n</em></td><td><code>td:gt(1)</code> finds cells after skipping the first two</td></tr>
    /// <tr><td><code>:eq(<em>n</em>)</code></td><td>elements whose sibling index is equal to <em>n</em></td><td><code>td:eq(0)</code> finds the first cell of each row</td></tr>
    /// <tr><td><code>:has(<em>selector</em>)</code></td><td>elements that contains at least one element matching the <em>selector</em></td><td><code>div:has(p)</code> finds divs that contain p elements </td></tr>
    /// <tr><td><code>:not(<em>selector</em>)</code></td><td>elements that do not match the <em>selector</em>. See also {@link Elements#not(String)}</td><code>div:not(.logo)</code> finds all divs that do not have the "logo" class</td></tr>
    /// <tr><td><code>:contains(<em>text</em>)</code></td><td>elements that contains the specified text. The search is case insensitive. The text may appear in the found element, or any of its descendants.</td><td><code>p:contains(jsoup)</code> finds p elements containing the text "jsoup".</td></tr>
    /// <tr><td><code>:matches(<em>regex</em>)</code></td><td>elements whose text matches the specified regular expression. The text may appear in the found element, or any of its descendants.</td><td><code>td:matches(\\d+)</code> finds table cells containing digits. <code>div:matches((?i)login)</code> finds divs containing the text, case insensitively.</td></tr>
    /// <tr><td><code>:containsOwn(<em>text</em>)</code></td><td>elements that directly contains the specified text. The search is case insensitive. The text must appear in the found element, not any of its descendants.</td><td><code>p:containsOwn(jsoup)</code> finds p elements with own text "jsoup".</td></tr>
    /// <tr><td><code>:matchesOwn(<em>regex</em>)</code></td><td>elements whose own text matches the specified regular expression. The text must appear in the found element, not any of its descendants.</td><td><code>td:matchesOwn(\\d+)</code> finds table cells directly containing digits. <code>div:matchesOwn((?i)login)</code> finds divs containing the text, case insensitively.</td></tr>
    /// <tr><td></td><td>The above may be combined in any order and with other selectors</td><td><code>.light:contains(name):eq(0)</code></td></tr>
    /// </table>
    /// </remarks>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    /// <see cref="Element.Select(string)"/>
    public class Selector
    {
        private readonly Evaluator _evaluator;
        private readonly Element _root;

        private Selector(string query, Element root)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }
            query = query.Trim();
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException("query");
            }
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }

            this._evaluator = QueryParser.Parse(query);

            this._root = root;
        }

        /// <summary>
        /// Find elements matching selector.
        /// </summary>
        /// <param name="query">CSS selector</param>
        /// <param name="root">root element to descend into</param>
        /// <returns>matching elements, empty if not</returns>
        public static Elements Select(string query, Element root)
        {
            return new Selector(query, root).Select();
        }

        /// <summary>
        /// Find elements matching selector.
        /// </summary>
        /// <param name="query">CSS selector</param>
        /// <param name="roots">root elements to descend into</param>
        /// <returns>matching elements, empty if not</returns>
        public static Elements Select(string query, IEnumerable<Element> roots)
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException("query");
            }
            if (roots == null)
            {
                throw new ArgumentNullException("root");
            }

            HashSet<Element> elements = new HashSet<Element>();

            foreach (Element root in roots)
            {
                foreach (Element item in Select(query, root))
                {
                    elements.Add(item);
                }

            }
            return new Elements(elements);
        }

        private Elements Select()
        {
            return Collector.Collect(_evaluator, _root);
        }

        // exclude set. package open so that Elements can implement .not() selector.
        public static Elements FilterOut(IEnumerable<Element> elements, IEnumerable<Element> outs)
        {
            Elements output = new Elements();
            foreach (Element el in elements)
            {
                bool found = false;
                foreach (Element outEl in outs)
                {
                    if (el.Equals(outEl))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    output.Add(el);
                }
            }
            return output;
        }

        public class SelectorParseException : Exception
        {
            public SelectorParseException(string s, params object[] paramaters)
                : base(string.Format(s, paramaters))
            {
            }
        }
    }
}