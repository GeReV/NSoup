using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NSoup.Nodes
{
    /// <summary>
    /// Evaluates that an element matches the selector.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    public abstract class Evaluator
    {
        private Evaluator() { }

        /// <summary>
        /// Test if the element meets the evaluator's requirements.
        /// </summary>
        /// <param name="element">Element to test.</param>
        /// <returns>True if element meets requirements.</returns>
        public abstract bool Matches(Element element);

        public sealed class Tag : Evaluator
        {
            private string tagName;

            public Tag(string tagName)
            {
                this.tagName = tagName;
            }

            public override bool Matches(Element element)
            {
                return (element.TagName.Equals(tagName));
            }
        }

        public sealed class Id : Evaluator
        {
            private string id;

            public Id(string id)
            {
                this.id = id;
            }

            public override bool Matches(Element element)
            {
                return (id.Equals(element.Id));
            }
        }

        public sealed class Class : Evaluator
        {
            private string className;

            public Class(string className)
            {
                this.className = className;
            }

            public override bool Matches(Element element)
            {
                return (element.HasClass(className));
            }
        }

        public sealed class Attribute : Evaluator
        {
            private string key;

            public Attribute(string key)
            {
                this.key = key;
            }

            public override bool Matches(Element element)
            {
                return element.HasAttr(key);
            }
        }

        public sealed class AttributeWithValue : AttributeKeyPair
        {
            public AttributeWithValue(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element element)
            {
                return element.HasAttr(key) && value.Equals(element.Attr(key), StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public sealed class AttributeWithValueNot : AttributeKeyPair
        {
            public AttributeWithValueNot(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element element)
            {
                return !value.Equals(element.Attr(key), StringComparison.InvariantCultureIgnoreCase);
            }
        }

        public sealed class AttributeWithValueStarting : AttributeKeyPair
        {
            public AttributeWithValueStarting(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element element)
            {
                return element.HasAttr(key) && element.Attr(key).ToLowerInvariant().StartsWith(value); // value is lower case already
            }
        }

        public sealed class AttributeWithValueEnding : AttributeKeyPair
        {
            public AttributeWithValueEnding(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element element)
            {
                return element.HasAttr(key) && element.Attr(key).ToLowerInvariant().EndsWith(value); // value is lower case
            }
        }

        public sealed class AttributeWithValueContaining : AttributeKeyPair
        {
            public AttributeWithValueContaining(string key, string value)
                : base(key, value)
            {
            }

            public override bool Matches(Element element)
            {
                return element.HasAttr(key) && element.Attr(key).ToLowerInvariant().Contains(value); // value is lower case
            }
        }

        public sealed class AttributeWithValueMatching : Evaluator {
        
            private string key;
            private Regex regex;

            public AttributeWithValueMatching(string key, Regex regex)
            {
                this.key = key.Trim().ToLowerInvariant();
                this.regex = regex;
            }

        public override bool Matches(Element element) {
            return element.HasAttr(key) && regex.IsMatch(element.Attr(key));
        }
    }

        public abstract class AttributeKeyPair : Evaluator
        {
            protected string key;
            protected string value;

            protected AttributeKeyPair(string key, string value)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentNullException("key");
                }
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException("value");
                }

                this.key = key.Trim().ToLowerInvariant();
                this.value = value.Trim().ToLowerInvariant();
            }
        }

        public sealed class AllElements : Evaluator
        {
            public override bool Matches(Element element)
            {
                return true;
            }
        }

        public sealed class IndexLessThan : IndexEvaluator
        {
            public IndexLessThan(int index)
                : base(index)
            {
            }

            public override bool Matches(Element element)
            {
                return element.ElementSiblingIndex < index;
            }
        }

        public sealed class IndexGreaterThan : IndexEvaluator
        {
            public IndexGreaterThan(int index)
                : base(index)
            {
            }

            public override bool Matches(Element element)
            {
                return element.ElementSiblingIndex > index;
            }
        }

        public sealed class IndexEquals : IndexEvaluator
        {
            public IndexEquals(int index)
                : base(index)
            {
            }

            public override bool Matches(Element element)
            {
                return element.ElementSiblingIndex == index;
            }
        }

        public abstract class IndexEvaluator : Evaluator
        {
            protected int index;

            protected IndexEvaluator(int index)
            {
                this.index = index;
            }
        }

        public sealed class ContainsText : Evaluator
        {
            private string searchText;

            public ContainsText(string searchText)
            {
                this.searchText = searchText.ToLowerInvariant();
            }

            public override bool Matches(Element element)
            {
                return (element.Text.ToLowerInvariant().Contains(searchText));
            }
        }

        public sealed class MatchesRegex : Evaluator
        {
            private Regex regex;

            public MatchesRegex(Regex regex)
            {
                this.regex = regex;
            }

            // Cannot name function same as class name.
            public override bool Matches(Element element)
            {
                return regex.IsMatch(element.Text);
            }
        }
    }
}
