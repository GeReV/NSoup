using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Parse
{
    /// <summary>
    /// Parses HTML into a <see cref="Document"/>. Generally best to use one of the  more convenient parse methods in <see cref="NSoupClient"/>.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    internal class Parser
    {
        /// <summary>
        /// Parse HTML into a Document. 
        /// </summary>
        /// <param name="html">HTML to parse</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>parsed Document</returns>
        public static Document Parse(string html, string baseUri)
        {
            TreeBuilder treeBuilder = new TreeBuilder();
            return treeBuilder.Parse(html, baseUri);
        }

        /// <summary>
        /// Parse a fragment of HTML into a list of nodes. The context element, if supplied, supplies parsing context.
        /// </summary>
        /// <param name="fragmentHtml">the fragment of HTML to parse</param>
        /// <param name="context">(optional) the element that this HTML fragment is being parsed for (i.e. for inner HTML). This provides stack context (for implicit element creation).</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>list of nodes parsed from the input HTML. Note that the context element, if supplied, is not modifed.</returns>
        public static IList<Node> ParseFragment(string fragmentHtml, Element context, string baseUri)
        {
            TreeBuilder treeBuilder = new TreeBuilder();
            return treeBuilder.ParseFragment(fragmentHtml, context, baseUri);
        }

        /// <summary>
        /// Parse a fragment of HTML into the <code>body</code> of a Document.
        /// </summary>
        /// <param name="bodyHtml">fragment of HTML</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>Document, with empty head, and HTML parsed into body</returns>
        public static Document ParseBodyFragment(string bodyHtml, string baseUri)
        {
            Document doc = Document.CreateShell(baseUri);
            Element body = doc.Body;

            IList<Node> nodeList = ParseFragment(bodyHtml, body, baseUri);
            Node[] nodes = nodeList.ToArray(); // the node list gets modified when re-parented

            foreach (Node node in nodes)
            {
                body.AppendChild(node);
            }

            return doc;
        }

        /// <summary>
        /// </summary>
        /// <param name="bodyHtml">HTML to parse</param>
        /// <param name="baseUri">baseUri base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>parsed Document</returns>
        [Obsolete("Use ParseBodyFragment() or ParseFragment() instead.")]
        public static Document ParseBodyFragmentRelaxed(String bodyHtml, String baseUri)
        {
            return Parse(bodyHtml, baseUri);
        }
    }
}