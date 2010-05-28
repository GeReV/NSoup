using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Nodes
{
    /// <summary>
    /// A comment node.
    /// </summary>
    /// <!--
    /// Original author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Comment : Node
    {
        private static readonly string COMMENT_KEY = "comment";

        /// <summary>
        /// Create a new comment node.
        /// </summary>
        /// <param name="data">The contents of the comment</param>
        /// <param name="baseUri">base URI</param>
        public Comment(string data, string baseUri)
            : base(baseUri)
        {
            Attributes.Add(COMMENT_KEY, data);
        }

        /// <summary>
        /// Gets the node's name.
        /// </summary>
        public override string NodeName
        {
            get { return "#comment"; }
        }

        /// <summary>
        /// Get the contents of the comment.
        /// </summary>
        /// <returns>Content</returns>
        public string GetData()
        {
            return Attributes.GetValue(COMMENT_KEY);
        }

        public override void CreateOuterHtml(StringBuilder accum)
        {
            Indent(accum);
            accum.Append(string.Format("<!--{0}-->", GetData()));
        } 

        /*private void OuterHtml(StringBuilder accum)
        {
            
        }*/

        public override string ToString()
        {
            return OuterHtml();
        }
    }
}
