using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Parse
{
    /// <summary>
    /// Parses HTML into a <see cref="Document"/>. Generally best to use one of the  more convenient parse methods in <see cref="NSoup"/>.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Parser
    {
        private static readonly string _SQ = "'";
        private static readonly string _DQ = "\"";

        private static readonly Tag _htmlTag = Tag.ValueOf("html");
        private static readonly Tag _headTag = Tag.ValueOf("head");
        private static readonly Tag _bodyTag = Tag.ValueOf("body");
        private static readonly Tag _titleTag = Tag.ValueOf("title");
        private static readonly Tag _textareaTag = Tag.ValueOf("textarea");

        private readonly LinkedList<Element> _stack;
        private readonly TokenQueue _tq;
        private readonly Document _doc;
        private string _baseUri;

        public bool Relaxed { get; set; }

        private Parser(string html, string baseUri, bool isBodyFragment)
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri");
            }

            Relaxed = false;

            _stack = new LinkedList<Element>();
            _tq = new TokenQueue(html);
            this._baseUri = baseUri;

            if (isBodyFragment)
            {
                _doc = Document.CreateShell(baseUri);
                _stack.AddLast(_doc.Body);
            }
            else
            {
                _doc = new Document(baseUri);
                _stack.AddLast(_doc);
            }
        }

        /// <summary>
        /// Parse HTML into a Document.
        /// </summary>
        /// <param name="html">HTML to parse</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>parsed Document</returns>
        public static Document Parse(string html, string baseUri)
        {
            Parser parser = new Parser(html, baseUri, false);
            return parser.Parse();
        }

        /// <summary>
        /// Parse a fragment of HTML into the <code>body</code> of a Document.
        /// </summary>
        /// <param name="bodyHtml">fragment of HTML</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>Document, with empty head, and HTML parsed into body</returns>
        public static Document ParseBodyFragment(string bodyHtml, string baseUri)
        {
            Parser parser = new Parser(bodyHtml, baseUri, true);
            return parser.Parse();
        }

        /// <summary>
        /// Parse a fragment of HTML into the {@code body} of a Document, with relaxed parsing enabled. Relaxed, in this 
        /// context, means that implicit tags are not automatically created when missing.
        /// </summary>
        /// <param name="bodyHtml">fragment of HTML</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>Document, with empty head, and HTML parsed into body</returns>
        public static Document ParseBodyFragmentRelaxed(string bodyHtml, string baseUri)
        {
            Parser parser = new Parser(bodyHtml, baseUri, true);
            parser.Relaxed = true;
            return parser.Parse();
        }

        private Document Parse()
        {
            while (!_tq.IsEmpty)
            {
                if (_tq.Matches("<!--"))
                {
                    ParseComment();
                }
                else if (_tq.Matches("<![CDATA["))
                {
                    ParseCdata();
                }
                else if (_tq.Matches("<?") || _tq.Matches("<!"))
                {
                    ParseXmlDecl();
                }
                else if (_tq.Matches("</"))
                {
                    ParseEndTag();
                }
                else if (_tq.Matches("<"))
                {
                    ParseStartTag();
                }
                else
                {
                    ParseTextNode();
                }
            }
            return _doc.Normalise();
        }

        private void ParseComment()
        {
            _tq.Consume("<!--");
            string data = _tq.ChompTo("->");

            if (data.EndsWith("-")) // i.e. was -->
                data = data.Substring(0, data.Length - 1);
            Comment comment = new Comment(data, _baseUri);
            Last.AppendChild(comment);
        }

        private void ParseXmlDecl()
        {
            _tq.Consume("<");
            char firstChar = _tq.Consume(); // <? or <!, from initial match.
            bool procInstr = firstChar.ToString().Equals("!");
            string data = _tq.ChompTo(">");

            XmlDeclaration decl = new XmlDeclaration(data, _baseUri, procInstr);
            Last.AppendChild(decl);
        }

        private void ParseEndTag()
        {
            _tq.Consume("</");
            string tagName = _tq.ConsumeTagName();
            _tq.ChompTo(">");

            if (!string.IsNullOrEmpty(tagName))
            {
                Tag tag = Tag.ValueOf(tagName);
                PopStackToClose(tag);
            }
        }

        private void ParseStartTag()
        {
            _tq.Consume("<");
            string tagName = _tq.ConsumeTagName();

            if (string.IsNullOrEmpty(tagName))
            { // doesn't look like a start tag after all; put < back on stack and handle as text
                _tq.AddFirst("&lt;");
                ParseTextNode();
                return;
            }

            _tq.ConsumeWhitespace();
            Attributes attributes = new Attributes();
            while (!_tq.MatchesAny("<", "/>", ">") && !_tq.IsEmpty)
            {
                NSoup.Nodes.Attribute attribute = ParseAttribute();
                if (attribute != null)
                {
                    attributes.Add(attribute);
                }
            }

            Tag tag = Tag.ValueOf(tagName);
            Element child = new Element(tag, _baseUri, attributes);

            bool isEmptyElement = tag.IsEmpty; // empty element if empty tag (e.g. img) or self-closed el (<div/>
            if (_tq.MatchChomp("/>"))
            { // close empty element or tag
                isEmptyElement = true;
                if (!tag.IsKnownTag) // if unknown and a self closed, allow it to be self closed on output. this doesn't force all instances to be empty
                {
                    tag.SetSelfClosing();
                }
            }
            else
            {
                _tq.MatchChomp(">");
            }
            AddChildToParent(child, isEmptyElement);

            // pc data only tags (textarea, script): chomp to end tag, add content as text node
            if (tag.IsData)
            {
                string data = _tq.ChompToIgnoreCase("</" + tagName);
                _tq.ChompTo(">");
                PopStackToClose(tag);

                Node dataNode;
                if (tag.Equals(_titleTag) || tag.Equals(_textareaTag))
                { // want to show as text, but not contain inside tags (so not a data tag?)
                    dataNode = TextNode.CreateFromEncoded(data, _baseUri);
                }
                else
                {
                    dataNode = new DataNode(data, _baseUri); // data not encoded but raw (for " in script)
                }
                child.AppendChild(dataNode);
            }

            // <base href>: update the base uri
            if (child.TagName.Equals("base"))
            {
                string href = child.AbsUrl("href");
                if (!string.IsNullOrEmpty(href))
                { // ignore <base target> etc
                    _baseUri = href;
                    _doc.BaseUri = href; // set on the doc so doc.createElement(Tag) will get updated base
                }
            }
        }

        private NSoup.Nodes.Attribute ParseAttribute()
        {
            _tq.ConsumeWhitespace();
            string key = _tq.ConsumeAttributeKey();
            string value = string.Empty;
            _tq.ConsumeWhitespace();
            if (_tq.MatchChomp("="))
            {
                _tq.ConsumeWhitespace();

                if (_tq.MatchChomp(_SQ))
                {
                    value = _tq.ChompTo(_SQ);
                }
                else if (_tq.MatchChomp(_DQ))
                {
                    value = _tq.ChompTo(_DQ);
                }
                else
                {
                    StringBuilder valueAccum = new StringBuilder();
                    // no ' or " to look for, so scan to end tag or space (or end of stream)
                    while (!_tq.MatchesAny("<", "/>", ">") && !_tq.MatchesWhitespace() && !_tq.IsEmpty)
                    {
                        valueAccum.Append(_tq.Consume());
                    }
                    value = valueAccum.ToString();
                }
                _tq.ConsumeWhitespace();
            }
            if (!string.IsNullOrEmpty(key))
                return NSoup.Nodes.Attribute.CreateFromEncoded(key, value);
            else
            {
                _tq.Consume(); // unknown char, keep popping so not get stuck
                return null;
            }
        }

        private void ParseTextNode()
        {
            string text = _tq.ConsumeTo("<");
            TextNode textNode = TextNode.CreateFromEncoded(text, _baseUri);
            Last.AppendChild(textNode);
        }

        private void ParseCdata()
        {
            _tq.Consume("<![CDATA[");
            string rawText = _tq.ChompTo("]]>");
            TextNode textNode = new TextNode(rawText, _baseUri); // constructor does not escape
            Last.AppendChild(textNode);
        }

        private Element AddChildToParent(Element child, bool isEmptyElement)
        {
            Element parent = PopStackToSuitableContainer(child.Tag);
            Tag childTag = child.Tag;
            bool validAncestor = StackHasValidParent(childTag);

            if (!validAncestor && !Relaxed)
            {
                // create implicit parent around this child
                Tag parentTag = childTag.GetImplicitParent();
                Element implicitEl = new Element(parentTag, _baseUri);
                // special case: make sure there's a head before putting in body
                if (child.Tag.Equals(_bodyTag))
                {
                    Element head = new Element(_headTag, _baseUri);
                    implicitEl.AppendChild(head);
                }
                implicitEl.AppendChild(child);

                // recurse to ensure somewhere to put parent
                Element root = AddChildToParent(implicitEl, false);
                if (!isEmptyElement)
                {
                    _stack.AddLast(child);
                }
                return root;
            }

            parent.AppendChild(child);

            if (!isEmptyElement)
                _stack.AddLast(child);
            return parent;
        }

        private bool StackHasValidParent(Tag childTag)
        {
            if (_stack.Count == 1 && childTag.Equals(_htmlTag))
            {
                return true; // root is valid for html node
            }

            if (childTag.RequiresSpecificParent)
            {
                return _stack.Last.Value.Tag.IsValidParent(childTag);
            }

            // otherwise, look up the stack for valid ancestors
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                Element el = _stack.ElementAt(i);
                Tag parent2 = el.Tag;
                if (parent2.IsValidAncestor(childTag))
                {
                    return true;
                }
            }
            return false;
        }

        private Element PopStackToSuitableContainer(Tag tag)
        {
            while (_stack.Count > 0)
            {
                if (Last.Tag.CanContain(tag))
                    return Last;
                else
                    _stack.RemoveLast();
            }
            return null;
        }

        private Element PopStackToClose(Tag tag)
        {
            // first check to see if stack contains this tag; if so pop to there, otherwise ignore
            int counter = 0;
            Element elToClose = null;
            for (int i = _stack.Count - 1; i > 0; i--)
            {
                counter++;

                Element el = _stack.ElementAt(i);
                Tag elTag = el.Tag;

                if (elTag.Equals(_bodyTag) || elTag.Equals(_htmlTag))
                { // once in body, don't close past body
                    break;
                }
                else if (elTag.Equals(tag))
                {
                    elToClose = el;
                    break;
                }
            }
            if (elToClose != null)
            {
                for (int i = 0; i < counter; i++)
                {
                    _stack.RemoveLast();
                }
            }
            return elToClose;
        }

        private Element Last
        {
            get
            {
                return _stack.Last.Value;
            }
        }
    }
}
