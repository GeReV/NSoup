using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Nodes
{
    /// <summary>
    /// An XML Declaration.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class XmlDeclaration : Node
    {
        private static readonly string DECL_KEY = "declaration";
        private readonly bool _isProcessingInstruction; // <! if true, <? if false, declaration (and last data char should be ?)

        /// <summary>
        /// Create a new XML declaration
        /// </summary>
        /// <param name="data">data</param>
        /// <param name="baseUri">base uri</param>
        /// <param name="isProcessingInstruction">is processing instruction</param>
        public XmlDeclaration(string data, string baseUri, bool isProcessingInstruction)
            : base(baseUri)
        {
            Attributes.Add(DECL_KEY, data);
            this._isProcessingInstruction = isProcessingInstruction;
        }

        /// <summary>
        /// Gets the node's name.
        /// </summary>
        public override string NodeName
        {
            get { return "#declaration"; }
        }

        /// <summary>
        /// Get the unencoded XML declaration.
        /// </summary>
        /// <returns>XML declaration</returns>
        public string GetWholeDeclaration()
        {
            return Attributes.GetValue(DECL_KEY);
        }

        public override void OuterHtmlHead(StringBuilder accum, int depth)
        {
            accum.Append(string.Format("<{0}{1}>", _isProcessingInstruction ? "!" : "?", GetWholeDeclaration()));
        }

        public override void OuterHtmlTail(StringBuilder accum, int depth) { }

        public override string ToString()
        {
            return OuterHtml();
        }
    }
}
