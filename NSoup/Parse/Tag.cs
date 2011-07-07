using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Parse
{
    /// <summary>
    ///  HTML Tag capabilities.
    /// </summary>
    /// <!--
    /// Original author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki, amirgrozki@gmail.com
    /// -->
    public class Tag : IEquatable<Tag>
    {
        private static readonly Dictionary<string, Tag> _tags = new Dictionary<string, Tag>();  // map of known tags

        static Tag()
        {
            Initialize();
        }

        private string _tagName;
        private bool _isBlock = true; // block or inline
        private bool _formatAsBlock = true; // should be formatted as a block
        private bool _canContainBlock = true; // Can this tag hold block level tags?
        private bool _canContainInline = true; // only pcdata if not
        private bool _empty = false; // can hold nothing; e.g. img
        private bool _selfClosing = false; // can self close (<foo />). used for unknown tags that self close, without forcing them as empty.
        private bool _preserveWhitespace = false; // for pre, textarea, script etc

        public Tag Parent { get; set; } // if not null, elements must be a direct child of parent

        private Tag(string tagName)
        {
            this._tagName = tagName.ToLowerInvariant();
        }

        public string Name
        {
            get { return _tagName; }
        }

        /// <summary>
        /// Gets a Tag by name. If not previously defined (unknown), returns a new generic tag, that can do anything.
        /// </summary>
        /// <remarks>
        /// Pre-defined tags (P, DIV etc) will be ==, but unknown tags are not registered and will only .Equals().
        /// </remarks>
        /// <param name="tagName">Name of tag, e.g. "p". Case insensitive.</param>
        /// <returns>The tag, either defined or new generic.</returns>
        public static Tag ValueOf(string tagName)
        {
            if (tagName == null)
            {
                throw new ArgumentNullException("tagName");
            }
            tagName = tagName.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(tagName))
            {
                throw new ArgumentException("tagName");
            }

            lock (_tags)
            {
                Tag tag = null;
                _tags.TryGetValue(tagName, out tag);
                if (tag == null)
                {
                    // not defined: create default; go anywhere, do anything! (incl be inside a <p>)
                    tag = new Tag(tagName);
                    tag._isBlock = false;
                    tag._canContainBlock = true;
                }
                return tag;
            }
        }

        /// <summary>
        /// Gets if this is a block tag.
        /// </summary>
        public bool IsBlock
        {
            get { return _isBlock; }
        }

        /// <summary>
        /// Gets if this tag should be formatted as a block (or as inline)
        /// </summary>
        public bool FormatAsBlock
        {
            get { return _formatAsBlock; }
        }

        /// <summary>
        /// Gets if this tag can contain block tags.
        /// </summary>
        public bool CanContainBlock
        {
            get { return _canContainBlock; }
        }

        /// <summary>
        /// Gets if this tag is an inline tag.
        /// </summary>
        public bool IsInline
        {
            get { return !_isBlock; }
        }

        /// <summary>
        /// Gets if this tag is a data only tag.
        /// </summary>
        public bool IsData
        {
            get { return !_canContainInline && !IsEmpty; }
        }

        /// <summary>
        /// Gets if this is an empty tag
        /// </summary>
        public bool IsEmpty
        {
            get { return _empty; }
        }

        /// <summary>
        /// Gets if this tag is self closing.
        /// </summary>
        /// <returns></returns>
        public bool IsSelfClosing
        {
            get { return _empty || _selfClosing; }
        }

        /// <summary>
        /// Gets if this is a pre-defined tag, or was auto created on parsing.
        /// </summary>
        public bool IsKnownTag()
        {
            return _tags.ContainsKey(_tagName);
        }

        /// <summary>
        /// Check if this tagname is a known tag.
        /// </summary>
        /// <param name="tagName">name of tag</param>
        /// <returns>if known HTML tag</returns>
        public static bool IsKnownTag(string tagName)
        {
            return _tags.ContainsKey(tagName);
        }

        /// <summary>
        /// Gets if this tag should preserve whitespace within child text nodes.
        /// </summary>
        public bool PreserveWhitespace
        {
            get { return _preserveWhitespace; }
        }

        public Tag SetSelfClosing()
        {
            _selfClosing = true;
            return this;
        }

        public override bool Equals(Object o)
        {
            if (this == o) return true;
            if (!(o is Tag)) return false;

            Tag tag = (Tag)o;

            if (CanContainBlock != tag.CanContainBlock) return false;
            if (_canContainInline != tag._canContainInline) return false;
            if (_empty != tag._empty) return false;
            if (FormatAsBlock != tag.FormatAsBlock) return false;
            if (IsBlock != tag.IsBlock) return false;
            if (PreserveWhitespace != tag.PreserveWhitespace) return false;
            if (_selfClosing != tag._selfClosing) return false;
            if (!_tagName.Equals(tag._tagName)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = _tagName.GetHashCode();

            result = 31 * result + (_isBlock ? 1 : 0);
            result = 31 * result + (_formatAsBlock ? 1 : 0);
            result = 31 * result + (_canContainBlock ? 1 : 0);
            result = 31 * result + (_canContainInline ? 1 : 0);
            result = 31 * result + (_empty ? 1 : 0);
            result = 31 * result + (_selfClosing ? 1 : 0);
            result = 31 * result + (_preserveWhitespace ? 1 : 0);

            return result;
        }

        public override string ToString()
        {
            return _tagName;
        }

        // internal static initialisers:
        // prepped from http://www.w3.org/TR/REC-html40/sgml/dtd.html and other sources
        private static readonly string[] blockTags = {
            "html", "head", "body", "frameset", "script", "noscript", "style", "meta", "link", "title", "frame",
            "noframes", "section", "nav", "aside", "hgroup", "header", "footer", "p", "h1", "h2", "h3", "h4", "h5", "h6",
            "ul", "ol", "pre", "div", "blockquote", "hr", "address", "figure", "figcaption", "form", "fieldset", "ins",
            "del", "dl", "dt", "dd", "li", "table", "caption", "thead", "tfoot", "tbody", "colgroup", "col", "tr", "th",
            "td", "video", "audio", "canvas", "details", "menu", "plaintext"
    };
        private static readonly string[] inlineTags = {
            "object", "base", "font", "tt", "i", "b", "u", "big", "small", "em", "strong", "dfn", "code", "samp", "kbd",
            "var", "cite", "abbr", "time", "acronym", "mark", "ruby", "rt", "rp", "a", "img", "br", "wbr", "map", "q",
            "sub", "sup", "bdo", "iframe", "embed", "span", "input", "select", "textarea", "label", "button", "optgroup",
            "option", "legend", "datalist", "keygen", "output", "progress", "meter", "area", "param", "source", "track",
            "summary", "command", "device"
    };
        private static readonly string[] emptyTags = {
            "meta", "link", "base", "frame", "img", "br", "wbr", "embed", "hr", "input", "keygen", "col", "command",
            "device"
    };
        private static readonly string[] formatAsInlineTags = {
            "title", "a", "p", "h1", "h2", "h3", "h4", "h5", "h6", "pre", "address", "li", "th", "td"
    };
        private static readonly string[] preserveWhitespaceTags = { "pre", "plaintext", "title" };

        static void Initialize()
        {
            // creates
            foreach (string tagName in blockTags)
            {
                Tag tag = new Tag(tagName);
                Register(tag);
            }

            foreach (string tagName in inlineTags)
            {
                Tag tag = new Tag(tagName);
                tag._isBlock = false;
                tag._canContainBlock = false;
                tag._formatAsBlock = false;
                Register(tag);
            }

            // mods:
            foreach (string tagName in emptyTags)
            {
                Tag tag = _tags[tagName];

                if (tag == null)
                {
                    throw new InvalidOperationException("tag is null.");
                }

                tag._canContainBlock = false;
                tag._canContainInline = false;
                tag._empty = true;
            }

            foreach (string tagName in formatAsInlineTags)
            {
                Tag tag = _tags[tagName];

                if (tag == null)
                {
                    throw new InvalidOperationException("tag is null.");
                }

                tag._formatAsBlock = false;
            }

            foreach (string tagName in preserveWhitespaceTags)
            {
                Tag tag = _tags[tagName];

                if (tag == null)
                {
                    throw new InvalidOperationException("tag is null.");
                }

                tag._preserveWhitespace = true;
            }
        }

        #region IEquatable<Tag> Members

        public bool Equals(Tag other)
        {
            return Equals(other as object);
        }

        #endregion

        private static Tag Register(Tag tag)
        {
            lock (_tags)
            {
                if (_tags.ContainsKey(tag._tagName))
                {
                    _tags[tag._tagName] = tag;
                }
                else
                {
                    _tags.Add(tag._tagName, tag);
                }
            }
            return tag;
        }
    }
}