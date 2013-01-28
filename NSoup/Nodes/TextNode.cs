using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using NSoup.Helper;

namespace NSoup.Nodes
{

    /// <summary>
    /// A text node.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class TextNode : Node
    {
        /*
        TextNode is a node, and so by default comes with attributes and children. The attributes are seldom used, but use
        memory, and the child nodes are never used. So we don't have them, and override accessors to attributes to create
        them as needed on the fly.
        */
        private static readonly string TEXT_KEY = "text";
        private string text;

        /// <summary>
        /// Create a new TextNode representing the supplied (unencoded) text).
        /// </summary>
        /// <param name="text">raw text</param>
        /// <param name="baseUri">base uri</param>
        /// <seealso cref="CreateFromEncoded(string, string)"/>
        public TextNode(string text, string baseUri)
        {
            this.BaseUri = baseUri;
            this.text = text;
        }

        protected TextNode() { } // Used for Node.Clone().

        public override string NodeName
        {
            get { return "#text"; }
        }

        /// <summary>
        /// Get the unencoded, normalised text content of this text node.
        /// </summary>
        /// <seealso cref="TextNode.GetWholeText()"/>
        public string Text()
        {
            return NormaliseWhitespace(GetWholeText());
        }

        /// <summary>
        /// Set the text content of this text node.
        /// </summary>
        /// <param name="text">unencoded text</param>
        /// <returns>this, for chaining</returns>
        public TextNode Text(string text)
        {
            this.text = text;
            if (Attributes != null)
            {
                Attributes[TEXT_KEY] = text;
            }
            return this;
        }

        /// <summary>
        /// Get the (unencoded) text of this text node, including any newlines and spaces present in the original.
        /// </summary>
        /// <returns>text</returns>
        public string GetWholeText()
        {
            return Attributes == null ? text : Attributes.GetValue(TEXT_KEY);
        }

        /// <summary>
        /// Test if this text node is blank -- that is, empty or only whitespace (including newlines).
        /// </summary>
        public bool IsBlank
        {
            get { return GetWholeText().IsBlank(); }
        }

        /// <summary>
        /// Split this text node into two nodes at the specified string offset. After splitting, this node will contain the 
        /// original text up to the offset, and will have a new text node sibling containing the text after the offset.
        /// </summary>
        /// <param name="offset">string offset point to split node at.</param>
        /// <returns>the newly created text node containing the text after the offset.</returns>
        public TextNode SplitText(int offset)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException("offset", "Split offset must be not be negative");
            }

            if (offset > text.Length)
            {
                throw new ArgumentOutOfRangeException("offset", "Split offset must not be greater than current text length");
            }

            string head = GetWholeText().Substring(0, offset);
            string tail = GetWholeText().Substring(offset);
            
            Text(head);

            TextNode tailNode = new TextNode(tail, this.BaseUri);
            if (ParentNode != null)
            {
                ParentNode.AddChildren(SiblingIndex + 1, tailNode);
            }

            return tailNode;
        }

        public override void OuterHtmlHead(StringBuilder accum, int depth, OutputSettings output)
        {
            string html = Entities.Escape(GetWholeText(), output);
            if (output.PrettyPrint() && ParentNode is Element && !((Element)ParentNode).PreserveWhitespace)
            {
                html = NormaliseWhitespace(html);
            }

            if (output.PrettyPrint() && SiblingIndex == 0 && ParentNode is Element && ((Element)ParentNode).Tag.FormatAsBlock && !IsBlank)
            {
                Indent(accum, depth, output);
            }
            accum.Append(html);
        }

        public override void OuterHtmlTail(StringBuilder accum, int depth, OutputSettings output) { }

        public override string ToString()
        {
            return OuterHtml();
        }

        /// <summary>
        /// Create a new TextNode from HTML encoded (aka escaped) data.
        /// </summary>
        /// <param name="encodedText">Text containing encoded HTML (e.g. &amp;lt;)</param>
        /// <param name="baseUri"></param>
        /// <returns>TextNode containing unencoded data (e.g. &lt;)</returns>
        public static TextNode CreateFromEncoded(string encodedText, string baseUri)
        {
            string text = Entities.Unescape(encodedText);
            return new TextNode(text, baseUri);
        }

        public static string NormaliseWhitespace(string text)
        {
            text = text.NormaliseWhitespace(); // more than one space, and newlines to " "
            return text;
        }

        public static string StripLeadingWhitespace(string text)
        {
            return text.TrimStart();
        }

        public static bool LastCharIsWhitespace(StringBuilder sb)
        {
            return sb.Length != 0 && sb[sb.Length - 1] == ' ';
        }

        // attribute fiddling. create on first access.
        private void EnsureAttributes()
        {
            if (_attributes == null)
            {
                this._attributes = new Attributes();
                _attributes[TEXT_KEY] = text;
            }
        }

        public override Attributes Attributes
        {
            get
            {
                EnsureAttributes();
                return base.Attributes;
            }
        }

        public override string Attr(string attributeKey)
        {
            EnsureAttributes();
            return base.Attr(attributeKey);
        }

        public override Node Attr(string attributeKey, string attributeValue)
        {
            EnsureAttributes();
            return base.Attr(attributeKey, attributeValue);
        }

        public override bool HasAttr(string attributeKey)
        {
            EnsureAttributes();
            return base.HasAttr(attributeKey);
        }


        public override Node RemoveAttr(string attributeKey)
        {
            EnsureAttributes();
            return base.RemoveAttr(attributeKey);
        }

        public override string AbsUrl(String attributeKey)
        {
            EnsureAttributes();
            return base.AbsUrl(attributeKey);
        }
    }
}