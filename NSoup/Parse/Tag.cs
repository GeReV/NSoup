using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Parse
{    
    /// <summary>
    ///  HTML Tag specifications. This is a very simplistic model without the full expressiveness as the DTD,
    /// but it should capture most of what we need to know to intelligently parse a doc.
    /// </summary>
    /// <!--
    /// Original author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki, amirgrozki@gmail.com
    /// -->
    public class Tag : IEquatable<Tag>
    {
        private static readonly Dictionary<string, Tag> _tags = new Dictionary<string, Tag>();
        private static readonly Tag _defaultAncestor;

        static Tag()
        {
            _defaultAncestor = new Tag("BODY");
            _tags.Add(_defaultAncestor._tagName, _defaultAncestor);

            Initialize();
        }

        private string _tagName;
        private bool _knownTag = false; // if pre-defined or auto-created
        private bool _isBlock = true; // block or inline
        private bool _canContainBlock = true; // Can this tag hold block level tags?
        private bool _canContainInline = true; // only pcdata if not
        private bool _optionalClosing = false; // If tag is open, and another seen, close previous tag
        private bool _empty = false; // can hold nothing; e.g. img
        private bool _selfClosing = false; // can self close (<foo />). used for unknown tags that self close, without forcing them as empty.
        private bool _preserveWhitespace = false; // for pre, textarea, script etc
        private List<Tag> _ancestors; // elements must be a descendant of one of these ancestors
        private List<Tag> _excludes = new List<Tag>(); // cannot contain these tags
        private bool _directDescendant; // if true, must directly descend from one of the ancestors
        private bool _limitChildren; // if true, only contain children that've registered parents

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
                    tag.SetAncestor(_defaultAncestor._tagName);
                    tag.SetExcludes();
                    tag._isBlock = false;
                    tag._canContainBlock = true;
                }
                return tag;
            }
        }

        /// <summary>
        /// Test if this tag, the prospective parent, can accept the proposed child.
        /// </summary>
        /// <param name="child">potential child tag.</param>
        /// <returns>true if this can contain child.</returns>
        public bool CanContain(Tag child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            if (child._isBlock && !this._canContainBlock)
                return false;

            if (!child._isBlock && !this._canContainInline) // not block == inline
                return false;

            if (this._optionalClosing && this.Equals(child))
                return false;

            if (this._empty || this.IsData)
                return false;

            // don't allow children to contain their parent (directly)
            if (this.RequiresSpecificParent && this.GetImplicitParent().Equals(child))
            {
                return false;
            }

            // confirm limited children
            if (_limitChildren)
            {
                foreach (Tag childParent in child._ancestors)
                {
                    if (childParent.Equals(this))
                    {
                        return true;
                    }
                }
                return false;
            }

            // exclude children
            if (_excludes.Count > 0)
            {
                foreach (Tag excluded in _excludes)
                {
                    if (child.Equals(excluded))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets if this is a block tag.
        /// </summary>
        public bool IsBlock
        {
            get { return _isBlock; }
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
        public bool IsKnownTag
        {
            get { return _knownTag; }
        }

        /// <summary>
        /// Gets if this tag should preserve whitespace within child text nodes.
        /// </summary>
        public bool PreserveWhitespace
        {
            get { return _preserveWhitespace; }
        }

        public Tag GetImplicitParent()
        {
            return (!(_ancestors.Count <= 0)) ? _ancestors[0] : null;
        }

        public bool RequiresSpecificParent
        {
            get { return _directDescendant; }
        }

        public bool IsValidParent(Tag child)
        {
            return IsValidAncestor(child);
        }

        public bool IsValidAncestor(Tag child)
        {
            if (child._ancestors == null || child._ancestors.Count <= 0)
            {
                return true; // HTML tag
            }

            foreach (Tag tag in child._ancestors)
            {
                if (this.Equals(tag))
                    return true;
            }
            return false;
        }

        public override bool Equals(Object o)
        {
            if (this == o) return true;
            if (o == null || this.GetType() != o.GetType()) return false;

            Tag tag = (Tag)o;

            if (_tagName != null ? !_tagName.Equals(tag._tagName) : tag._tagName != null) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = _tagName != null ? _tagName.GetHashCode() : 0;
            result = 31 * result + (_isBlock ? 1 : 0);
            result = 31 * result + (_canContainBlock ? 1 : 0);
            result = 31 * result + (_canContainInline ? 1 : 0);
            result = 31 * result + (_optionalClosing ? 1 : 0);
            result = 31 * result + (_empty ? 1 : 0);
            return result;
        }

        public override string ToString()
        {
            return _tagName;
        }

        // internal static initialisers:

        static void Initialize()
        {
            // prepped from http://www.w3.org/TR/REC-html40/sgml/dtd.html#inline
            // tags are set here in uppercase for legibility, but internally held as lowercase.
            // TODO[must]: incorporate html 5 as appropriate

            // document
            CreateBlock("HTML").SetAncestor(new string[0]); // specific includes not impl
            CreateBlock("HEAD").SetParent("HTML").SetLimitChildren();
            CreateBlock("BODY").SetAncestor("HTML"); // specific includes not impl
            CreateBlock("FRAMESET").SetAncestor("HTML");

            // head
            // all ancestors set to (head, body): so implicitly create head, but allow in body
            CreateBlock("SCRIPT").SetAncestor("HEAD", "BODY").SetContainDataOnly();
            CreateBlock("NOSCRIPT").SetAncestor("HEAD", "BODY");
            CreateBlock("STYLE").SetAncestor("HEAD", "BODY").SetContainDataOnly();
            CreateBlock("META").SetAncestor("HEAD", "BODY").SetEmpty();
            CreateBlock("LINK").SetAncestor("HEAD", "BODY").SetEmpty(); // only within head
            CreateInline("OBJECT").SetAncestor("HEAD", "BODY"); // flow (block/inline) or param
            CreateBlock("TITLE").SetAncestor("HEAD", "BODY").SetContainDataOnly();
            CreateInline("BASE").SetAncestor("HEAD", "BODY").SetEmpty();

            CreateBlock("FRAME").SetParent("FRAMESET").SetEmpty();
            CreateBlock("NOFRAMES").SetParent("FRAMESET").SetContainDataOnly();

            // html5 sections
            CreateBlock("SECTION");
            CreateBlock("NAV");
            CreateBlock("ASIDE");
            CreateBlock("HGROUP").SetLimitChildren(); // limited to h1 - h6
            CreateBlock("HEADER").SetExcludes("HEADER", "FOOTER");
            CreateBlock("FOOTER").SetExcludes("HEADER", "FOOTER");

            // fontstyle
            CreateInline("FONT");
            CreateInline("TT");
            CreateInline("I");
            CreateInline("B");
            CreateInline("BIG");
            CreateInline("SMALL");

            // phrase
            CreateInline("EM");
            CreateInline("STRONG");
            CreateInline("DFN").SetOptionalClosing();
            CreateInline("DFN");
            CreateInline("CODE");
            CreateInline("SAMP");
            CreateInline("KBD");
            CreateInline("VAR");
            CreateInline("CITE");
            CreateInline("ABBR");
            CreateInline("TIME").SetOptionalClosing();
            CreateInline("ACRONYM");
            CreateInline("MARK");

            // ruby
            CreateInline("RUBY");
            CreateInline("RT").SetParent("RUBY").SetExcludes("RT", "RP");
            CreateInline("RP").SetParent("RUBY").SetExcludes("RT", "RP");

            // special
            CreateInline("A").SetOptionalClosing(); // cannot contain self
            CreateInline("IMG").SetEmpty();
            CreateInline("BR").SetEmpty();
            CreateInline("WBR").SetEmpty();
            CreateInline("MAP"); // map is defined as inline, but can hold block (what?) or area. Seldom used so NBD.
            CreateInline("Q");
            CreateInline("SUB");
            CreateInline("SUP");
            CreateInline("SPAN");
            CreateInline("BDO");
            CreateInline("IFRAME").SetOptionalClosing();
            CreateInline("EMBED").SetEmpty();

            // things past this point aren't really blocks or inline. I'm using them because they can hold block or inline,
            // but per the spec, only specific elements can hold this. if this becomes a real-world parsing problem,
            // will need to have another non block/inline type, and explicit include & exclude rules. should be right though

            // block
            CreateBlock("P").SetContainInlineOnly(); // emasculated block?
            CreateBlock("H1").SetAncestor("BODY", "HGROUP").SetContainInlineOnly();
            CreateBlock("H2").SetAncestor("BODY", "HGROUP").SetContainInlineOnly();
            CreateBlock("H3").SetAncestor("BODY", "HGROUP").SetContainInlineOnly();
            CreateBlock("H4").SetAncestor("BODY", "HGROUP").SetContainInlineOnly();
            CreateBlock("H5").SetAncestor("BODY", "HGROUP").SetContainInlineOnly();
            CreateBlock("H6").SetAncestor("BODY", "HGROUP").SetContainInlineOnly();
            CreateBlock("UL");
            CreateBlock("OL");
            CreateBlock("PRE").SetContainInlineOnly().SetPreserveWhitespace();
            CreateBlock("DIV");
            CreateBlock("BLOCKQUOTE");
            CreateBlock("HR").SetEmpty();
            CreateBlock("ADDRESS").SetContainInlineOnly();
            CreateBlock("FIGURE");
            CreateBlock("FIGCAPTION").SetAncestor("FIGURE");


            // formctrl
            CreateBlock("FORM").SetOptionalClosing(); // can't contain self
            CreateInline("INPUT").SetAncestor("FORM").SetEmpty();
            CreateInline("SELECT").SetAncestor("FORM"); // just contain optgroup or option
            CreateInline("TEXTAREA").SetAncestor("FORM").SetContainDataOnly();
            CreateInline("LABEL").SetAncestor("FORM").SetOptionalClosing(); // not self
            CreateInline("BUTTON").SetAncestor("FORM"); // bunch of excludes not defined
            CreateInline("OPTGROUP").SetParent("SELECT"); //  only contain option
            CreateInline("OPTION").SetParent("SELECT", "OPTGROUP", "DATALIST").SetOptionalClosing();
            CreateBlock("FIELDSET").SetAncestor("FORM");
            CreateInline("LEGEND").SetAncestor("FIELDSET");

            // html5 form ctrl, not specced to have to be in forms
            CreateInline("DATALIST");
            CreateInline("KEYGEN").SetEmpty();
            CreateInline("OUTPUT");
            CreateInline("PROGRESS").SetOptionalClosing();
            CreateInline("METER").SetOptionalClosing();

            // other
            CreateInline("AREA").SetAncestor("MAP").SetEmpty(); // not an inline per-se
            CreateInline("PARAM").SetParent("OBJECT").SetEmpty();
            CreateBlock("INS"); // only within body
            CreateBlock("DEL"); // only within body

            // definition lists. per spec, dt and dd are inline and must directly descend from dl. However in practise
            // these are all used as blocks and dl need only be an ancestor
            CreateBlock("DL").SetOptionalClosing(); // can't nest
            CreateBlock("DT").SetAncestor("DL").SetExcludes("DL", "DD").SetOptionalClosing(); // only within DL.
            CreateBlock("DD").SetAncestor("DL").SetExcludes("DL", "DT").SetOptionalClosing(); // only within DL.

            CreateBlock("LI").SetAncestor("UL", "OL").SetOptionalClosing(); // only within OL or UL.

            // tables
            CreateBlock("TABLE"); // specific list of only includes (tr, td, thead etc) not implemented
            CreateBlock("CAPTION").SetParent("TABLE").SetExcludes("THEAD", "TFOOT", "TBODY", "COLGROUP", "COL", "TR", "TH", "TD").SetOptionalClosing();
            CreateBlock("THEAD").SetParent("TABLE").SetLimitChildren().SetOptionalClosing(); // just TR
            CreateBlock("TFOOT").SetParent("TABLE").SetLimitChildren().SetOptionalClosing(); // just TR
            CreateBlock("TBODY").SetParent("TABLE").SetLimitChildren().SetOptionalClosing(); // optional / implicit open too. just TR
            CreateBlock("COLGROUP").SetParent("TABLE").SetLimitChildren().SetOptionalClosing(); // just COL
            CreateBlock("COL").SetParent("COLGROUP").SetEmpty();
            CreateBlock("TR").SetParent("TBODY", "THEAD", "TFOOT", "TABLE").SetLimitChildren().SetOptionalClosing(); // just TH, TD
            CreateBlock("TH").SetParent("TR").SetExcludes("THEAD", "TFOOT", "TBODY", "COLGROUP", "COL", "TR", "TH", "TD").SetOptionalClosing();
            CreateBlock("TD").SetParent("TR").SetExcludes("THEAD", "TFOOT", "TBODY", "COLGROUP", "COL", "TR", "TH", "TD").SetOptionalClosing();

            // html5 media
            CreateBlock("VIDEO").SetExcludes("VIDEO", "AUDIO");
            CreateBlock("AUDIO").SetExcludes("VIDEO", "AUDIO");
            CreateInline("SOURCE").SetParent("VIDEO", "AUDIO").SetEmpty();
            CreateInline("TRACK").SetParent("VIDEO", "AUDIO").SetEmpty();
            CreateBlock("CANVAS");

            // html5 interactive
            CreateBlock("DETAILS");
            CreateInline("SUMMARY").SetParent("DETAILS");
            CreateInline("COMMAND").SetEmpty();
            CreateBlock("MENU");
            CreateInline("DEVICE").SetEmpty();
        }

        #region IEquatable<Tag> Members

        public bool Equals(Tag other)
        {
            return Equals(other as object);
        }

        #endregion

        private static Tag CreateBlock(string tagName)
        {
            return Register(new Tag(tagName));
        }

        private static Tag CreateInline(string tagName)
        {
            Tag inline = new Tag(tagName);
            inline._isBlock = false;
            inline._canContainBlock = false;
            return Register(inline);
        }

        private static Tag Register(Tag tag)
        {
            tag.SetAncestor(_defaultAncestor._tagName);
            tag.SetKnownTag();
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

        private Tag SetContainInlineOnly()
        {
            _canContainBlock = false;
            _canContainInline = true;
            return this;
        }

        private Tag SetContainDataOnly()
        {
            _canContainBlock = false;
            _canContainInline = false;
            _preserveWhitespace = true;
            return this;
        }

        private Tag SetEmpty()
        {
            _canContainBlock = false;
            _canContainInline = false;
            _empty = true;
            return this;
        }

        private Tag SetOptionalClosing()
        {
            _optionalClosing = true;
            return this;
        }

        private Tag SetPreserveWhitespace()
        {
            _preserveWhitespace = true;
            return this;
        }

        private Tag SetAncestor(params string[] tagNames)
        {
            if (tagNames == null || tagNames.Length == 0)
            {
                _ancestors = new List<Tag>();
            }
            else
            {
                _ancestors = new List<Tag>(tagNames.Length);
                foreach (string name in tagNames)
                {
                    _ancestors.Add(Tag.ValueOf(name));
                }
            }
            return this;
        }

        private Tag SetExcludes(params string[] tagNames)
        {
            if (tagNames == null || tagNames.Length == 0)
            {
                _excludes = new List<Tag>();
            }
            else
            {
                _excludes = new List<Tag>(tagNames.Length);
                foreach (string name in tagNames)
                {
                    _excludes.Add(Tag.ValueOf(name));
                }
            }
            return this;
        }

        private Tag SetParent(params string[] tagNames)
        {
            _directDescendant = true;
            SetAncestor(tagNames);
            return this;
        }

        private Tag SetLimitChildren()
        {
            _limitChildren = true;
            return this;
        }

        public Tag SetSelfClosing()
        {
            _selfClosing = true;
            return this;
        }

        private Tag SetKnownTag()
        {
            _knownTag = true;
            return this;
        }
    }
}
