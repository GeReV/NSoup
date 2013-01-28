using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace NSoup.Nodes
{
    /// <summary>
    /// A data node, for contents of style, script tags etc, where contents should not show in text().
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class DataNode : Node
    {
        private static readonly string DATA_KEY = "data";

        /// <summary>
        /// Create a new DataNode.
        /// </summary>
        /// <param name="data">data contents</param>
        /// <param name="baseUri">base URI</param>
        public DataNode(string data, string baseUri)
            : base(baseUri)
        {
            Attributes.Add(DATA_KEY, data);
        }

        /// <summary>
        /// Gets the node's name.
        /// </summary>
        public override string NodeName
        {
            get { return "#data"; }
        }

        /// <summary>
        /// Get the data contents of this node. Will be unescaped and with original new lines, space etc.
        /// </summary>
        /// <returns>data</returns>
        public string GetWholeData()
        {
            return Attributes.GetValue(DATA_KEY);
        }

        /// <summary>
        /// Set the data contents of this node.
        /// </summary>
        /// <param name="data">unencoded data</param>
        /// <returns>this node, for chaining</returns>
        public DataNode setWholeData(string data)
        {
            _attributes[DATA_KEY] = data;
            return this;
        }

        public override void OuterHtmlHead(StringBuilder accum, int depth, OutputSettings output)
        {
            accum.Append(GetWholeData()); // data is not escaped in return from data nodes, so " in script, style is plain
        }

        public override void OuterHtmlTail(StringBuilder accum, int depth, OutputSettings output) { }

        public override string ToString()
        {
            return OuterHtml();
        }

        /// <summary>
        /// Create a new DataNode from HTML encoded data.
        /// </summary>
        /// <param name="encodedData">encoded data</param>
        /// <param name="baseUri">bass URI</param>
        /// <returns>new DataNode</returns>
        public static DataNode CreateFromEncoded(string encodedData, string baseUri)
        {
            string data = Entities.Unescape(encodedData);
            return new DataNode(data, baseUri);
        }
    }
}
