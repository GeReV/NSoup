﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;

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
        private static readonly string TEXT_KEY = "text";

        /// <summary>
        /// Create a new TextNode representing the supplied (unencoded) text).
        /// </summary>
        /// <param name="text">raw text</param>
        /// <param name="baseUri">base uri</param>
        /// <seealso cref="CreateFromEncoded(string, string)"/>
        public TextNode(string text, string baseUri)
            : base(baseUri)
        {
            Attributes.Add(TEXT_KEY, text);
        }

        public override string NodeName
        {
            get { return "#text"; }
        }

        /// <summary>
        /// Get the (unencoded) text of this text node, including any newlines and spaces present in the original.
        /// </summary>
        /// <returns>text</returns>
        public string GetWholeText()
        {
            return Attributes.GetValue(TEXT_KEY);
        }

        /// <summary>
        /// Test if this text node is blank -- that is, empty or only whitespace (including newlines).
        /// </summary>
        public bool IsBlank
        {
            get { return string.IsNullOrEmpty(NormaliseWhitespace(GetWholeText())); }
        }

        public override void CreateOuterHtml(StringBuilder accum)
        {
            string html = HttpUtility.HtmlEncode(GetWholeText());
            if (ParentNode is Element && !((Element)ParentNode).PreserveWhitespace)
            {
                html = NormaliseWhitespace(html);
            }

            if (!IsBlank && ParentNode is Element && ((Element)ParentNode).Tag.CanContainBlock && SiblingIndex == 0)
            {
                Indent(accum);
            }
            accum.Append(html);
        }

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
            string text = HttpUtility.HtmlDecode(encodedText);
            return new TextNode(text, baseUri);
        }

        public static string NormaliseWhitespace(string text)
        {
            text = Regex.Replace(text, "\\s{2,}|(\\r\\n|\\r|\\n)", " "); // more than one space, and newlines to " "
            return text;
        }

        public static string StripLeadingWhitespace(string text)
        {
            return text.TrimStart();
        }

        public static bool LastCharIsWhitespace(StringBuilder sb)
        {
            if (sb.Length == 0)
            {
                return false;
            }
            string lastChar = sb[sb.Length - 1].ToString();
            //Validate.isTrue(lastChar.length() == 1); // todo: remove check
            return lastChar.Equals(" ");
        }
    }
}