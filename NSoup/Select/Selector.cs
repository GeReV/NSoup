using System;
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
    /// <tr><td><code>E</code></td><td>an element of type E</td><td><code>h1</code></td></tr>
    /// <tr><td><code>E#id</code></td><td>an Element with attribute ID of "id"</td><td><code>div#wrap</code>, <code>#logo</code></td></tr>
    /// <tr><td><code>E.class</code></td><td>an Element with a class name of "class"</td><td><code>div.left</code>, <code>.result</code></td></tr>
    /// <tr><td><code>E[attr]</code></td><td>an Element with the attribute named "attr"</td><td><code>a[href]</code>, <code>[title]</code></td></tr>
    /// <tr><td><code>E[attr=val]</code></td><td>an Element with the attribute named "attr" and value equal to "val"</td><td><code>img[width=500]</code>, <code>a[rel=nofollow]</code></td></tr>
    /// <tr><td><code>E[attr^=val]</code></td><td>an Element with the attribute named "attr" and value starting with "val"</td><td><code>a[href^=http:]</code></code></td></tr>
    /// <tr><td><code>E[attr$=val]</code></td><td>an Element with the attribute named "attr" and value ending with "val"</td><td><code>img[src$=.png]</code></td></tr>
    /// <tr><td><code>E[attr*=val]</code></td><td>an Element with the attribute named "attr" and value containing "val"</td><td><code>a[href*=/search/]</code></td></tr>
    /// <tr><td></td><td>The above may be combined in any order</td><td><code>div.header[title]</code></td></tr>
    /// <tr><td><td colspan="3"><h3>Combinators</h3></td></tr>
    /// <tr><td><code>E F</code></td><td>an F element descended from an E element</td><td><code>div a</code>, <code>.logo h1</code></td></tr>
    /// <tr><td><code>E > F</code></td><td>an F child of E</td><td><code>ol > li</code></td></tr>
    /// <tr><td><code>E + F</code></td><td>an F element immediately preceded by sibling E</td><td><code>li + li</code>, <code>div.head + div</code></td></tr>
    /// <tr><td><code>E ~ F</code></td><td>an F element preceded by sibling E</td><td><code>h1 ~ p</code></td></tr>
    /// <tr><td><code>E, F, G</code></td><td>any matching element E, F, or G</td><td><code>a[href], div, h3</code></td></tr>
    /// <tr><td><td colspan="3"><h3>Pseudo selectors</h3></td></tr>
    /// <tr><td><code>E:lt(<em>n</em>)</code></td><td>an Element whose sibling index is less than <em>n</em></td><td><code>td:lt(3)</code> finds the first 2 cells of each row</td></tr>
    /// <tr><td><code>E:gt(<em>n</em>)</code></td><td>an Element whose sibling index is greater than <em>n</em></td><td><code>td:gt(1)</code> finds cells after skipping the first two</td></tr>
    /// <tr><td><code>E:eq(<em>n</em>)</code></td><td>an Element whose sibling index is equal to <em>n</em></td><td><code>td:eq(1)</code> finds the first cell of each row</td></tr>
    /// <tr><td><code>E:has(<em>selector</em>)</code></td><td>an Element that contains at least one element matching the <em>selector</em></td><td><code>div:has(p)</code> finds divs that contain p elements </td></tr>
    /// <tr><td><code>E:contains(<em>text</em>)</code></td><td>an Element that contains the specified text. The search is case insensitive. The text may appear in the found Element, or any of its descendants.</td><td><code>p:contains(jsoup)</code> finds p elements containing the text "jsoup".</td></tr>
    /// </table>
    /// </remarks>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Selector
    {
        private readonly static string[] combinators = { ",", ">", "+", "~", " " };
        private readonly Element _root;
        private List<Element> _elements; // LHS for unique and ordered elements
        private readonly string _query;
        private readonly TokenQueue _tq;

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

            

            this._elements = new List<Element>();
            this._query = query;
            this._root = root;
            this._tq = new TokenQueue(query);
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
            _tq.ConsumeWhitespace();

            if (_tq.MatchesAny(combinators))
            { // if starts with a combinator, use root as elements
                _elements.Add(_root);
                Combinator(_tq.Consume().ToString());
            }
            else
            {
                AddElements(FindElements()); // chomp first element matcher off queue 
            }

            while (!_tq.IsEmpty)
            {
                // hierarchy and extras
                bool seenWhite = _tq.ConsumeWhitespace();

                if (_tq.MatchChomp(","))
                { // group or
                    while (!_tq.IsEmpty)
                    {
                        string subQuery = _tq.ChompTo(",");

                        foreach (Element item in Select(subQuery, _root))
                        {
                            _elements.Add(item);
                        }
                    }
                }
                else if (_tq.MatchesAny(combinators))
                {
                    Combinator(_tq.Consume().ToString());
                }
                else if (seenWhite)
                {
                    Combinator(" ");
                }
                else
                { // E.class, E#id, E[attr] etc. AND
                    Elements candidates = FindElements(); // take next el, #. etc off queue
                    IntersectElements(FilterForSelf(_elements, candidates));
                }
            }
            return new Elements(_elements);
        }

        private void Combinator(string combinator)
        {
            _tq.ConsumeWhitespace();
            string subQuery = _tq.ConsumeToAny(combinators); // support multi > childs

            Elements output;
            if (combinator.Equals(">"))
                output = FilterForChildren(_elements, Select(subQuery, _elements));
            else if (combinator.Equals(" "))
                output = FilterForDescendants(_elements, Select(subQuery, _elements));
            else if (combinator.Equals("+"))
                output = FilterForAdjacentSiblings(_elements, Select(subQuery, _root));
            else if (combinator.Equals("~"))
                output = FilterForGeneralSiblings(_elements, Select(subQuery, _root));
            else
                throw new Exception("Unknown combinator: " + combinator);

            _elements.Clear();
            foreach (Element item in output)
            {
                _elements.Add(item);
            }

        }

        private Elements FindElements()
        {
            if (_tq.MatchChomp("#"))
            {
                return ById();
            }
            else if (_tq.MatchChomp("."))
            {
                return ByClass();
            }
            else if (_tq.MatchesWord())
            {
                return ByTag();
            }
            else if (_tq.MatchChomp("["))
            {
                return ByAttribute();
            }
            else if (_tq.MatchChomp("*"))
            {
                return AllElements();
            } else if (_tq.MatchChomp(":lt(")) {
            return IndexLessThan();
            }
            else if (_tq.MatchChomp(":gt("))
            {
                return IndexGreaterThan();
            }
            else if (_tq.MatchChomp(":eq("))
            {
                return IndexEquals();
            }
            else if (_tq.MatchChomp(":has("))
            {
                return Has();
            }
            else if (_tq.MatchChomp(":contains("))
            {
                return Contains();
            }
            else
            { // unhandled
                throw new SelectorParseException("Could not parse query " + _query);
            }
        }

        private void AddElements(ICollection<Element> add)
        {
            foreach (Element item in add)
            {
                _elements.Add(item);
            }
        }

        private void IntersectElements(ICollection<Element> intersect)
        {
            _elements = _elements.Intersect(intersect).ToList();
        }

        private Elements ById()
        {
            string id = _tq.ConsumeCssIdentifier();

            if (string.IsNullOrEmpty(id))
            {
                throw new Exception("Id is empty.");
            }

            Element found = _root.GetElementById(id);
            Elements byId = new Elements();
            if (found != null)
            {
                byId.Add(found);
            }
            return byId;
        }

        private Elements ByClass()
        {
            string className = _tq.ConsumeCssIdentifier();

            if (string.IsNullOrEmpty(className))
            {
                throw new Exception("className is empty.");
            }

            return _root.GetElementsByClass(className);
        }

        private Elements ByTag()
        {
            string tagName = _tq.ConsumeWord();

            if (string.IsNullOrEmpty(tagName))
            {
                throw new Exception("tagName is empty.");
            }

            return _root.GetElementsByTag(tagName);
        }

        private Elements ByAttribute()
        {
            string key = _tq.ConsumeToAny("=", "!=", "^=", "$=", "*=", "]"); // eq, not, start, end, contain, (no val)

            if (string.IsNullOrEmpty(key))
            {
                throw new Exception("key is empty.");
            }

            if (_tq.MatchChomp("]"))
            {
                return _root.GetElementsByAttribute(key);
            }
            else
            {
                if (_tq.MatchChomp("="))
                {
                    return _root.GetElementsByAttributeValue(key, _tq.ChompTo("]"));
                }
                else if (_tq.MatchChomp("!="))
                {
                    return _root.GetElementsByAttributeValueNot(key, _tq.ChompTo("]"));
                }
                else if (_tq.MatchChomp("^="))
                {
                    return _root.GetElementsByAttributeValueStarting(key, _tq.ChompTo("]"));
                }
                else if (_tq.MatchChomp("$="))
                {
                    return _root.GetElementsByAttributeValueEnding(key, _tq.ChompTo("]"));
                }
                else if (_tq.MatchChomp("*="))
                {
                    return _root.GetElementsByAttributeValueContaining(key, _tq.ChompTo("]"));
                }
                else
                {
                    throw new SelectorParseException("Could not parse attribute query " + _query);
                }
            }
        }

        private Elements AllElements()
        {
            return _root.GetAllElements();
        }

        // pseudo selectors :lt, :gt, :eq
        private Elements IndexLessThan()
        {
            return _root.GetElementsByIndexLessThan(ConsumeIndex());
        }

        private Elements IndexGreaterThan()
        {
            return _root.GetElementsByIndexGreaterThan(ConsumeIndex());
        }

        private Elements IndexEquals()
        {
            return _root.GetElementsByIndexEquals(ConsumeIndex());
        }

        private int ConsumeIndex()
        {
            string indexS = _tq.ChompTo(")").Trim();
            int index = 0;

            if (!int.TryParse(indexS, out index))
            {
                throw new InvalidOperationException("Index must be numeric");
            }

            return index;
        }

        // pseudo selector :has(el)
        private Elements Has()
        {
            String subQuery = _tq.ChompTo(")");

            if (string.IsNullOrEmpty(subQuery))
            {
                throw new Exception(":has(el) subselect must not be empty");
            }

            return FilterForParentsOfDescendants(_elements, Select(subQuery, _elements));
        }
    
        // pseudo selector :contains(text)
        private Elements Contains()
        {
            String searchText = _tq.ChompTo(")");

            if (string.IsNullOrEmpty(searchText))
            {
                throw new Exception(":contains(text) query must not be empty");
            }

            return _root.GetElementsContainingText(searchText);
        }

        // direct child descendants
        private static Elements FilterForChildren(IList<Element> parents, IList<Element> candidates)
        {
            Elements children = new Elements();

            for (int i = 0; i < candidates.Count; i++)
            {
                Element c = candidates[i];
                bool skipStep = false;

                for (int j = 0; j < parents.Count && (!skipStep); j++)
                {
                    Element p = parents[j];
                    if (c.Parent != null && c.Parent.Equals(p))
                    {
                        children.Add(c);

                        // continue upper loop;
                        skipStep = true;
                    }
                }
            }

            return children;
        }

        // children or lower descendants. input candidates stemmed from found elements, so are either a descendant 
        // or the original element; so check that parent is not child
        private static Elements FilterForDescendants(IList<Element> parents, IList<Element> candidates)
        {
            Elements children = new Elements();

            for (int i = 0; i < candidates.Count; i++)
            {
                Element c = candidates[i];
                bool found = false;
                for (int j = 0; j < parents.Count && (!found); j++)
                {
                    Element p = parents[j];
                    if (c.Equals(p))
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    children.Add(c);
                }
            }
            return children;
        }

        // implements :has(el). Finds elements that contain the matched elements
        private static Elements FilterForParentsOfDescendants(ICollection<Element> parents, ICollection<Element> children)
        {
            Elements filteredParents = new Elements();
            foreach (Element p in parents)
            {
                foreach (Element c in children)
                {
                    if (c.Parents.Contains(p))
                    {
                        filteredParents.Add(p);
                    }
                }
            }
            return filteredParents;
        }

        // adjacent siblings
        private static Elements FilterForAdjacentSiblings(IList<Element> elements, IList<Element> candidates)
        {
            Elements siblings = new Elements();

            for (int i = 0; i < candidates.Count; i++)
            {
                Element c = candidates[i];
                bool skipStep = false;
                for (int j = 0; j < elements.Count && (!skipStep); j++)
                {
                    Element e = elements[j];
                    if (!e.Parent.Equals(c.Parent))
                    {
                        continue;
                    }
                    Element previousSib = c.PreviousElementSibling;
                    if (previousSib != null && previousSib.Equals(e))
                    {
                        siblings.Add(c);
                        skipStep = true;
                    }
                }
            }

            return siblings;
        }

        // preceeding siblings
        private static Elements FilterForGeneralSiblings(IList<Element> elements, IList<Element> candidates)
        {
            Elements output = new Elements();

            for (int i = 0; i < candidates.Count; i++)
            {
                Element c = candidates[i];
                bool skipStep = false;
                for (int j = 0; j < elements.Count && (!skipStep); j++)
                {
                    Element e = elements[j];
                    if (!e.Parent.Equals(c.Parent))
                    {
                        continue;
                    }

                    int ePos = e.ElementSiblingIndex;
                    int cPos = c.ElementSiblingIndex;
                    if (cPos > ePos)
                    {
                        output.Add(c);
                        skipStep = true;
                    }
                }
            }
            return output;
        }

        // union of both sets, for e.class type selectors
        private static Elements FilterForSelf(IList<Element> parents, IList<Element> candidates)
        {
            Elements children = new Elements();

            for (int i = 0; i < candidates.Count; i++)
            {
                Element c = candidates[i];
                bool skipStep = false;
                
                for (int j = 0; j < parents.Count && (!skipStep); j++)
                {
                    Element p = parents[j];
                    if (c.Equals(p))
                    {
                        children.Add(c);
                        skipStep = true;
                    }
                }
            }

            return children;
        }

        public class SelectorParseException : Exception
        {
            public SelectorParseException(string s)
                : base(s)
            {
            }
        }
    }
}
