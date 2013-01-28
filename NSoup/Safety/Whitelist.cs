using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Safety
{
    /// <summary>
    /// Whitelists define what HTML (elements and attributes) to allow through the cleaner. Everything else is removed.
    /// </summary>
    /// <remarks>
    /// Start with one of the defaults: 
    /// <ul> 
    /// <li><see cref="None"/></li>
    /// <li><see cref="SimpleText"/></li>
    /// <li><see cref="Basic"/></li>
    /// <li><see cref="BasicWithImages"/></li>
    /// <li><see cref="Relaxed"/></li>
    /// </ul> 
    /// If you need to allow more through (please be careful!), tweak a base whitelist with: 
    /// <ul> 
    /// <li>{@link #addTags}    
    /// <li>{@link #addAttributes} 
    /// <li>{@link #addEnforcedAttribute} 
    /// <li>{@link #addProtocols} 
    /// </ul> 
    /// The cleaner and these whitelists assume that you want to clean a <code>body</code> fragment of HTML (to add user 
    /// supplied HTML into a templated page), and not to clean a full HTML document. If the latter is the case, either wrap the 
    /// document HTML around the cleaned body HTML, or create a whitelist that allows <code>html</code> and <code>head</code> 
    /// elements as appropriate. 
    /// 
    /// If you are going to extend a whitelist, please be very careful. Make sure you understand what attributes may lead to 
    /// XSS attack vectors. URL attributes are particularly vulnerable and require careful validation. See 
    /// http://ha.ckers.org/xss.html for some XSS attack examples.
    /// </remarks>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Whitelist
    {
        // Originally: Set<TagName>.
        private HashSet<TagName> _tagNames; // tags allowed, lower case. e.g. [p, br, span]
        private Dictionary<TagName, HashSet<AttributeKey>> _attributes; // tag -> attribute[]. allowed attributes [href] for a tag.
        private Dictionary<TagName, Dictionary<AttributeKey, AttributeValue>> _enforcedAttributes; // always set these attribute values
        // Originally: Map<TagName, Map<AttributeKey, Set<Protocol>>>.
        private Dictionary<TagName, Dictionary<AttributeKey, HashSet<Protocol>>> _protocols; // allowed URL protocols for attributes
        private bool _preserveRelativeLinks; // option to preserve relative links

        /// <summary>
        /// This whitelist allows only text nodes: all HTML will be stripped.
        /// </summary>
        public static Whitelist None
        {
            get { return new Whitelist(); }
        }

        /// <summary>
        /// This whitelist allows only simple text formatting: <code>b, em, i, strong, u</code>. All other HTML (tags and attributes) will be removed.
        /// </summary>
        public static Whitelist SimpleText
        {
            get
            {
                return new Whitelist()
                  .AddTags("b", "em", "i", "strong", "u");
            }
        }

        /// <summary>
        /// This whitelist allows a fuller range of text nodes: <code>a, b, blockquote, br, cite, code, dd, dl, dt, em, i, li, 
        /// ol, p, pre, q, small, strike, strong, sub, sup, u, ul</code>, and appropriate attributes.
        /// </summary>
        /// <remarks>
        /// Links (<code>a</code> elements) can point to <code>http, https, ftp, mailto</code>, and have an enforced 
        /// <code>rel=nofollow</code> attribute. 
        /// Does not allow images.
        /// </remarks>
        public static Whitelist Basic
        {
            get
            {
                return new Whitelist()
                    .AddTags(
                            "a", "b", "blockquote", "br", "cite", "code", "dd", "dl", "dt", "em",
                            "i", "li", "ol", "p", "pre", "q", "small", "strike", "strong", "sub",
                            "sup", "u", "ul")

                    .AddAttributes("a", "href")
                    .AddAttributes("blockquote", "cite")
                    .AddAttributes("q", "cite")

                    .AddProtocols("a", "href", "ftp", "http", "https", "mailto")
                    .AddProtocols("blockquote", "cite", "http", "https")
                    .AddProtocols("cite", "cite", "http", "https")

                    .AddEnforcedAttribute("a", "rel", "nofollow");
            }
        }

        /// <summary>
        /// This whitelist allows the same text tags as {@link #basic}, and also allows <code>img</code> tags, with appropriate 
        /// attributes, with <code>src</code> pointing to <code>http</code> or <code>https</code>.
        /// </summary>
        public static Whitelist BasicWithImages
        {
            get
            {
                return Basic
                    .AddTags("img")
                    .AddAttributes("img", "align", "alt", "height", "src", "title", "width")
                    .AddProtocols("img", "src", "http", "https");
            }
        }

        /// <summary>
        /// This whitelist allows a full range of text and structural body HTML: <code>a, b, blockquote, br, caption, cite, 
        /// code, col, colgroup, dd, dl, dt, em, h1, h2, h3, h4, h5, h6, i, img, li, ol, p, pre, q, small, strike, strong, sub, 
        /// sup, table, tbody, td, tfoot, th, thead, tr, u, ul</code> 
        /// </summary>
        /// <remarks>Links do not have an enforced <code>rel=nofollow</code> attribute, but you can add that if desired.</remarks>
        public static Whitelist Relaxed
        {
            get
            {
                return new Whitelist()
                        .AddTags(
                                "a", "b", "blockquote", "br", "caption", "cite", "code", "col",
                                "colgroup", "dd", "div", "dl", "dt", "em", "h1", "h2", "h3", "h4", "h5", "h6",
                                "i", "img", "li", "ol", "p", "pre", "q", "small", "strike", "strong",
                                "sub", "sup", "table", "tbody", "td", "tfoot", "th", "thead", "tr", "u",
                                "ul")

                        .AddAttributes("a", "href", "title")
                        .AddAttributes("blockquote", "cite")
                        .AddAttributes("col", "span", "width")
                        .AddAttributes("colgroup", "span", "width")
                        .AddAttributes("img", "align", "alt", "height", "src", "title", "width")
                        .AddAttributes("ol", "start", "type")
                        .AddAttributes("q", "cite")
                        .AddAttributes("table", "summary", "width")
                        .AddAttributes("td", "abbr", "axis", "colspan", "rowspan", "width")
                        .AddAttributes(
                                "th", "abbr", "axis", "colspan", "rowspan", "scope",
                                "width")
                        .AddAttributes("ul", "type")

                        .AddProtocols("a", "href", "ftp", "http", "https", "mailto")
                        .AddProtocols("blockquote", "cite", "http", "https")
                        .AddProtocols("img", "src", "http", "https")
                        .AddProtocols("q", "cite", "http", "https");
            }
        }

        /// <summary>
        /// Create a new, empty whitelist. Generally it will be better to start with a default prepared whitelist instead.
        /// </summary>
        /// <seealso cref="Basic"/>
        /// <seealso cref="BasicWithImages"/>
        /// <seealso cref="SimpleText"/>
        /// <seealso cref="Relaxed"/>
        public Whitelist()
        {
            _tagNames = new HashSet<TagName>();
            _attributes = new Dictionary<TagName, HashSet<AttributeKey>>();
            _enforcedAttributes = new Dictionary<TagName, Dictionary<AttributeKey, AttributeValue>>();
            _protocols = new Dictionary<TagName, Dictionary<AttributeKey, HashSet<Protocol>>>();
            _preserveRelativeLinks = false;
        }

        /// <summary>
        /// Add a list of allowed elements to a whitelist. (If a tag is not allowed, it will be removed from the HTML.)
        /// </summary>
        /// <param name="tags">tag names to allow</param>
        /// <returns>this (for chaining)</returns>
        public Whitelist AddTags(params string[] tags)
        {
            if (tags == null)
            {
                throw new ArgumentNullException("tags");
            }

            foreach (string tagName in tags)
            {
                if (string.IsNullOrEmpty(tagName))
                {
                    throw new Exception("An empty tag was found.");
                }
                _tagNames.Add(TagName.ValueOf(tagName));
            }
            return this;
        }

        /// <summary>
        /// Add a list of allowed attributes to a tag. (If an attribute is not allowed on an element, it will be removed.)
        /// E.g.: AddAttributes("a", "href", "class") allows href and class attributes on a tags.
        /// </summary>
        /// <remarks>
        /// To make an attribute valid for <b>all tags</b>, use the pseudo tag <code>:all</code>, e.g. 
        /// <code>AddAttributes(":all", "class")</code>.
        /// </remarks>
        /// <param name="tag">The tag the attributes are for. The tag will be added to the allowed tag list if necessary.</param>
        /// <param name="keys">List of valid attributes for the tag.</param>
        /// <returns>This (for chaining)</returns>
        public Whitelist AddAttributes(string tag, params string[] keys)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentNullException("tag");
            }
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            if (keys.Length <= 0)
            {
                throw new ArgumentException("No attributes supplied.");
            }

            TagName tagName = TagName.ValueOf(tag);
            if (!_tagNames.Contains(tagName))
            {
                _tagNames.Add(tagName);
            }
            HashSet<AttributeKey> attributeSet = new HashSet<AttributeKey>();
            foreach (string key in keys)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new Exception("key");
                }
                attributeSet.Add(AttributeKey.ValueOf(key));
            }
            if (_attributes.ContainsKey(tagName))
            {
                HashSet<AttributeKey> currentSet = _attributes[tagName];
                foreach (AttributeKey item in attributeSet)
                {
                    currentSet.Add(item);
                }

            }
            else
            {
                _attributes.Add(tagName, attributeSet);
            }
            return this;
        }

        /// <summary>
        /// Add an enforced attribute to a tag. An enforced attribute will always be added to the element. If the element 
        /// already has the attribute set, it will be overridden.
        /// </summary>
        /// <remarks>E.g.: <code>AddEnforcedAttribute("a", "rel", "nofollow")</code> will make all <code>a</code> tags output as 
        /// <code>&lt;a href="..." rel="nofollow"&gt;</code></remarks>
        /// <param name="tag">The tag the enforced attribute is for. The tag will be added to the allowed tag list if necessary.</param>
        /// <param name="key">The attribute key</param>
        /// <param name="value">The enforced attribute value</param>
        /// <returns>this (for chaining)</returns>
        public Whitelist AddEnforcedAttribute(string tag, string key, string value)
        {
            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentNullException("tag");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException("value");
            }

            TagName tagName = TagName.ValueOf(tag);
            if (!_tagNames.Contains(tagName))
            {
                _tagNames.Add(tagName);
            }
            AttributeKey attrKey = AttributeKey.ValueOf(key);
            AttributeValue attrVal = AttributeValue.ValueOf(value);

            if (_enforcedAttributes.ContainsKey(tagName))
            {
                _enforcedAttributes[tagName].Add(attrKey, attrVal);
            }
            else
            {
                Dictionary<AttributeKey, AttributeValue> attrMap = new Dictionary<AttributeKey, AttributeValue>();
                attrMap.Add(attrKey, attrVal);
                _enforcedAttributes.Add(tagName, attrMap);
            }
            return this;
        }

        /// <summary>
        /// Configure this Whitelist to preserve relative links in an element's URL attribute, or convert them to absolute
        /// links. By default, this is false: URLs will be  made absolute (e.g. start with an allowed protocol, like
        /// e.g. "http://".
        /// 
        /// Note that when handling relative links, the input document must have an appropriate base URI set when
        /// parsing, so that the link's protocol can be confirmed. Regardless of the setting of the preserve relative
        /// links option, the link must be resolvable against the base URI to an allowed protocol; otherwise the attribute
        /// will be removed.
        /// </summary>
        /// <param name="preserve">True to allow relative links, false (default) to deny</param>
        /// <returns>This Whitelist, for chaining.</returns>
        /// <see cref="AddProtocols()"/>
        public Whitelist PreserveRelativeLinks(bool preserve)
        {
            _preserveRelativeLinks = preserve;
            return this;
        }

        /// <summary>
        /// Add allowed URL protocols for an element's URL attribute. This restricts the possible values of the attribute to 
        /// URLs with the defined protocol.
        /// </summary>
        /// <remarks>E.g.: <code>AddProtocols("a", "href", "ftp", "http", "https")</code></remarks>
        /// <param name="tag">Tag the URL protocol is for</param>
        /// <param name="key">Attribute key</param>
        /// <param name="protocols">List of valid protocols</param>
        /// <returns>this, for chaining</returns>
        public Whitelist AddProtocols(string tag, string key, params string[] protocols)
        {

            if (string.IsNullOrEmpty(tag))
            {
                throw new ArgumentNullException("tag");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            if (protocols == null)
            {
                throw new ArgumentNullException("protocols");
            }

            TagName tagName = TagName.ValueOf(tag);
            AttributeKey attrKey = AttributeKey.ValueOf(key);
            Dictionary<AttributeKey, HashSet<Protocol>> attrMap;
            HashSet<Protocol> protSet;

            if (this._protocols.ContainsKey(tagName))
            {
                attrMap = this._protocols[tagName];
            }
            else
            {
                attrMap = new Dictionary<AttributeKey, HashSet<Protocol>>();
                this._protocols.Add(tagName, attrMap);
            }
            if (attrMap.ContainsKey(attrKey))
            {
                protSet = attrMap[attrKey];
            }
            else
            {
                protSet = new HashSet<Protocol>();
                attrMap.Add(attrKey, protSet);
            }
            foreach (string protocol in protocols)
            {
                if (string.IsNullOrEmpty(protocol))
                {
                    throw new Exception("protocol is empty.");
                }
                Protocol prot = Protocol.ValueOf(protocol);
                protSet.Add(prot);
            }
            return this;
        }

        public bool IsSafeTag(string tag)
        {
            return _tagNames.Contains(TagName.ValueOf(tag));
        }

        public bool IsSafeAttribute(string tagName, Element el, NSoup.Nodes.Attribute attr)
        {
            TagName tag = TagName.ValueOf(tagName);
            AttributeKey key = AttributeKey.ValueOf(attr.Key);

            if (_attributes.ContainsKey(tag))
            {
                if (_attributes[tag].Contains(key))
                {
                    if (_protocols.ContainsKey(tag))
                    {
                        Dictionary<AttributeKey, HashSet<Protocol>> attrProts = _protocols[tag];
                        // ok if not defined protocol; otherwise test
                        return !attrProts.ContainsKey(key) || TestValidProtocol(el, attr, attrProts[key]);
                    }
                    else
                    { // attribute found, no protocols defined, so OK
                        return true;
                    }
                }
            }

            // no attributes defined for tag, try :all tag
            return !tagName.Equals(":all") && IsSafeAttribute(":all", el, attr);
        }

        private bool TestValidProtocol(Element el, NSoup.Nodes.Attribute attr, HashSet<Protocol> protocols)
        {
            // try to resolve relative urls to abs, and optionally update the attribute so output html has abs.
            // rels without a baseuri get removed
            string value = el.AbsUrl(attr.Key);
            if (value.Length == 0)
            {
                value = attr.Value; // if it could not be made abs, run as-is to allow custom unknown protocols
            }
            if (!_preserveRelativeLinks)
            {
                attr.Value = value;
            }

            foreach (Protocol protocol in protocols)
            {
                string prot = protocol.ToString() + ":";
                if (value.ToLowerInvariant().StartsWith(prot))
                {
                    return true;
                }
            }
            return false;
        }

        public Attributes GetEnforcedAttributes(string tagName)
        {
            Attributes attrs = new Attributes();
            TagName tag = TagName.ValueOf(tagName);
            if (_enforcedAttributes.ContainsKey(tag))
            {
                Dictionary<AttributeKey, AttributeValue> keyVals = _enforcedAttributes[tag];
                foreach (KeyValuePair<AttributeKey, AttributeValue> entry in keyVals)
                {
                    attrs.Add(entry.Key.ToString(), entry.Value.ToString());
                }
            }
            return attrs;
        }

        // named types for config. All just hold strings, but here for my sanity.

        class TagName : TypedValue
        {
            TagName(string value)
                : base(value)
            {
            }

            public static TagName ValueOf(string value)
            {
                return new TagName(value);
            }
        }

        class AttributeKey : TypedValue
        {
            AttributeKey(string value)
                : base(value)
            {
            }

            public static AttributeKey ValueOf(string value)
            {
                return new AttributeKey(value);
            }
        }

        class AttributeValue : TypedValue
        {
            AttributeValue(string value)
                : base(value)
            {
            }

            public static AttributeValue ValueOf(string value)
            {
                return new AttributeValue(value);
            }
        }

        class Protocol : TypedValue
        {
            Protocol(string value)
                : base(value)
            {
            }

            public static Protocol ValueOf(string value)
            {
                return new Protocol(value);
            }
        }

        abstract class TypedValue
        {
            private string value;

            protected TypedValue(string value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                this.value = value;
            }

            public override int GetHashCode()
            {
                const int prime = 31;
                int result = 1;
                result = prime * result + ((value == null) ? 0 : value.GetHashCode());
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj) return true;
                if (obj == null) return false;
                if (this.GetType() != obj.GetType()) return false;
                TypedValue other = (TypedValue)obj;
                if (value == null)
                {
                    if (other.value != null) return false;
                }
                else if (!value.Equals(other.value)) return false;
                return true;
            }

            public override string ToString()
            {
                return value;
            }
        }
    }
}
