using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NSoup.Nodes;

namespace NSoup.Select
{
    /// <summary>
    /// Evaluates that an element matches the selector.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    internal abstract class Evaluator
    {

        protected Evaluator()
        {
        }

        /// <summary>
        /// Test if the element meets the evaluator's requirements.
        /// </summary>
        /// <param name="root">Root of the matching subtree</param>
        /// <param name="element">tested element</param>
        /// <returns></returns>
        public abstract bool Matches(Element root, Element element);

        /// <summary>
        /// Evaluator for tag name
        /// </summary>
        public sealed class Tag : Evaluator
        {
            private string tagName;

            public Tag(String tagName)
            {
                this.tagName = tagName;
            }


            public override bool Matches(Element root, Element element)
            {
                return (element.TagName().Equals(tagName));
            }


            public override string ToString()
            {
                return tagName;
            }
        }

        /// <summary>
        /// Evaluator for element id
        /// </summary>
        public sealed class Id : Evaluator
        {
            private string id;

            public Id(string id)
            {
                this.id = id;
            }


            public override bool Matches(Element root, Element element)
            {
                return (id.Equals(element.Id));
            }


            public override string ToString()
            {
                return id;
            }

        }

        /// <summary>
        /// Evaluator for element class
        /// </summary>
        public sealed class Class : Evaluator
        {
            private string className;

            public Class(string className)
            {
                this.className = className;
            }

            public override bool Matches(Element root, Element element)
            {
                return (element.HasClass(className));
            }

            public override string ToString()
            {
                return string.Format(".{0}", className);
            }
        }

        /// <summary>
        /// Evaluator for attibute name matching
        /// </summary>
        public sealed class Attribute : Evaluator
        {
            private string key;

            public Attribute(string key)
            {
                this.key = key;
            }


            public override bool Matches(Element root, Element element)
            {
                return element.HasAttr(key);
            }


            public override string ToString()
            {
                return string.Format("[{0}]", key);
            }

        }

        /// <summary>
        /// Evaluator for attribute name prefix matching
        /// </summary>
        public sealed class AttributeStarting : Evaluator
        {
            private string keyPrefix;

            public AttributeStarting(string keyPrefix)
            {
                this.keyPrefix = keyPrefix;
            }

            public override bool Matches(Element root, Element element)
            {
                System.Collections.ObjectModel.ReadOnlyCollection<NSoup.Nodes.Attribute> values = element.Attributes.AsList();

                foreach (NSoup.Nodes.Attribute attribute in values)
                {
                    if (attribute.Key.StartsWith(keyPrefix))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override string ToString()
            {
                return string.Format("[^{0}]", keyPrefix);
            }
        }

        /// <summary>
        /// Evaluator for attribute name/value matching
        /// </summary>
        public sealed class AttributeWithValue : AttributeKeyPair
        {
            public AttributeWithValue(string key, string value)
                : base(key, value)
            {
            }


            public override bool Matches(Element root, Element element)
            {
                return element.HasAttr(key) && value.Equals(element.Attr(key), StringComparison.InvariantCultureIgnoreCase);
            }


            public override string ToString()
            {
                return string.Format("[{0}={1}]", key, value);
            }

        }

        /// <summary>
        /// Evaluator for attribute name != value matching
        /// </summary>
        public sealed class AttributeWithValueNot : AttributeKeyPair
        {
            public AttributeWithValueNot(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element root, Element element)
            {
                return !value.Equals(element.Attr(key), StringComparison.InvariantCultureIgnoreCase);
            }

            public override string ToString()
            {
                return string.Format("[{0}!={1}]", key, value);
            }
        }

        /// <summary>
        /// Evaluator for attribute name/value matching (value prefix)
        /// </summary>
        public sealed class AttributeWithValueStarting : AttributeKeyPair
        {
            public AttributeWithValueStarting(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element root, Element element)
            {
                return element.HasAttr(key) && element.Attr(key).ToLowerInvariant().StartsWith(value); // value is lower case already
            }

            public override string ToString()
            {
                return string.Format("[{0}^={1}]", key, value);
            }
        }

        /// <summary>
        /// Evaluator for attribute name/value matching (value ending)
        /// </summary>
        public sealed class AttributeWithValueEnding : AttributeKeyPair
        {
            public AttributeWithValueEnding(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element root, Element element)
            {
                return element.HasAttr(key) && element.Attr(key).ToLowerInvariant().EndsWith(value); // value is lower case
            }

            public override string ToString()
            {
                return string.Format("[{0}$={1}]", key, value);
            }
        }

        /// <summary>
        /// Evaluator for attribute name/value matching (value containing)
        /// </summary>
        public sealed class AttributeWithValueContaining : AttributeKeyPair
        {
            public AttributeWithValueContaining(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element root, Element element)
            {
                return element.HasAttr(key) && element.Attr(key).ToLowerInvariant().Contains(value); // value is lower case
            }

            public override string ToString()
            {
                return string.Format("[{0}*={1}]", key, value);
            }
        }

        /// <summary>
        /// Evaluator for attribute name/value matching (value regex matching)
        /// </summary>
        public sealed class AttributeWithValueMatching : Evaluator
        {
            string key;
            Regex pattern;

            public AttributeWithValueMatching(string key, Regex pattern)
            {
                this.key = key.Trim().ToLowerInvariant();
                this.pattern = pattern;
            }

            public override bool Matches(Element root, Element element)
            {
                return element.HasAttr(key) && pattern.IsMatch(element.Attr(key));
            }

            public override string ToString()
            {
                return string.Format("[{0}~={1}]", key, pattern.ToString());
            }
        }

        /// <summary>
        /// Abstract evaluator for attribute name/value matching
        /// </summary>
        public abstract class AttributeKeyPair : Evaluator
        {
            protected string key;
            protected string value;

            public AttributeKeyPair(string key, string value)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Key cannot be empty.", "key");
                }

                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Key cannot be empty.", "key");
                }

                this.key = key.Trim().ToLowerInvariant();
                this.value = value.Trim().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Evaluator for any / all element matching
        /// </summary>
        public sealed class AllElements : Evaluator
        {
            public override bool Matches(Element root, Element element)
            {
                return true;
            }

            public override string ToString()
            {
                return "*";
            }
        }

        /// <summary>
        /// Evaluator for matching by sibling index number (e < idx)
        /// </summary>
        public sealed class IndexLessThan : IndexEvaluator
        {
            public IndexLessThan(int index)
                : base(index)
            {
            }

            public override bool Matches(Element root, Element element)
            {
                return element.ElementSiblingIndex < index;
            }

            public override string ToString()
            {
                return string.Format(":lt({0})", index);
            }
        }

        /// <summary>
        /// Evaluator for matching by sibling index number (e > idx)
        /// </summary>
        public sealed class IndexGreaterThan : IndexEvaluator
        {
            public IndexGreaterThan(int index)
                : base(index)
            {
            }

            public override bool Matches(Element root, Element element)
            {
                return element.ElementSiblingIndex > index;
            }

            public override string ToString()
            {
                return string.Format(":gt({0})", index);
            }
        }

        /// <summary>
        /// Evaluator for matching by sibling index number (e = idx)
        /// </summary>
        public sealed class IndexEquals : IndexEvaluator
        {
            public IndexEquals(int index)
                : base(index)
            {
            }

            public override bool Matches(Element root, Element element)
            {
                return element.ElementSiblingIndex == index;
            }

            public override string ToString()
            {
                return string.Format(":eq({0})", index);
            }
        }

        /// <summary>
        /// Abstract evaluator for sibling index matching
        /// </summary>
        public abstract class IndexEvaluator : Evaluator
        {
            protected int index;

            public IndexEvaluator(int index)
            {
                this.index = index;
            }
        }

        /// <summary>
        /// Evaluator for matching Element (and its descendents) text
        /// </summary>
        public sealed class ContainsText : Evaluator
        {
            private string searchText;

            public ContainsText(string searchText)
            {
                this.searchText = searchText.ToLowerInvariant();
            }

            public override bool Matches(Element root, Element element)
            {
                return (element.Text().ToLowerInvariant().Contains(searchText));
            }

            public override string ToString()
            {
                return string.Format(":contains({0}", searchText);
            }
        }

        /// <summary>
        /// Evaluator for matching Element's own text
        /// </summary>
        public sealed class ContainsOwnText : Evaluator
        {
            private string searchText;

            public ContainsOwnText(string searchText)
            {
                this.searchText = searchText.ToLowerInvariant();
            }

            public override bool Matches(Element root, Element element)
            {
                return (element.OwnText().ToLowerInvariant().Contains(searchText));
            }

            public override string ToString()
            {
                return string.Format(":containsOwn({0}", searchText);
            }
        }

        /// <summary>
        /// Evaluator for matching Element (and its descendents) text with regex
        /// </summary>
        public sealed class MatchesRegex : Evaluator
        {
            private Regex pattern;

            public MatchesRegex(Regex pattern)
            {
                this.pattern = pattern;
            }

            public override bool Matches(Element root, Element element)
            {
                return pattern.IsMatch(element.Text());
            }

            public override string ToString()
            {
                return string.Format(":matches({0}", pattern);
            }
        }

        /// <summary>
        /// Evaluator for matching Element's own text with regex
        /// </summary>
        public sealed class MatchesOwn : Evaluator
        {
            private Regex pattern;

            public MatchesOwn(Regex pattern)
            {
                this.pattern = pattern;
            }

            public override bool Matches(Element root, Element element)
            {
                return pattern.IsMatch(element.OwnText());
            }

            public override string ToString()
            {
                return string.Format(":matchesOwn({0}", pattern);
            }
        }
    }
}
