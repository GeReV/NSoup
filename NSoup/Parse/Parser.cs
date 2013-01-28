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
    public class Parser
    {
        private static readonly int DEFAULT_MAX_ERRORS = 0; // by default, error tracking is disabled.
    
        private TreeBuilder _treeBuilder;
        private int _maxErrors = DEFAULT_MAX_ERRORS;
        private ParseErrorList _errors;

        /// <summary>
        /// Create a new Parser, using the specified TreeBuilder
        /// </summary>
        /// <param name="treeBuilder">TreeBuilder to use to parse input into Documents.</param>
        private Parser(TreeBuilder treeBuilder) {
            this._treeBuilder = treeBuilder;
        }
    
        public Document ParseInput(string html, string baseUri) {
            _errors = IsTrackErrors ? ParseErrorList.Tracking(_maxErrors) : ParseErrorList.NoTracking();
            Document doc = _treeBuilder.Parse(html, baseUri, _errors);
            return doc;
        }

        // gets & sets
        /// <summary>
        /// Gets the TreeBuilder currently in use.
        /// </summary>
        public TreeBuilder TreeBuilder()
        {
            return _treeBuilder;
        }

        /// <summary>
        /// Update the TreeBuilder used when parsing content.
        /// </summary>
        /// <param name="treeBuilder">Current TreeBuilder</param>
        /// <returns>this, for chaining</returns>
        public Parser TreeBuilder(TreeBuilder treeBuilder) {
            this._treeBuilder = treeBuilder;
            return this;
        }

        /// <summary>
        /// Check if parse error tracking is enabled.
        /// </summary>
        public bool IsTrackErrors
        {
            get { return _maxErrors > 0; }
        }

        /// <summary>
        /// Enable or disable parse error tracking for the next parse.
        /// </summary>
        /// <param name="maxErrors">The maximum number of errors to track. Set to 0 to disable.</param>
        /// <returns>this, for chaining</returns>
        public Parser SetTrackErrors(int maxErrors) {
            this._maxErrors = maxErrors;
            return this;
        }

        /// <summary>
        /// Retrieve the parse errors, if any, from the last parse.
        /// </summary>
        /// <returns>List of parse errors, up to the size of the maximum errors tracked.</returns>
        public List<ParseError> GetErrors() {
            return _errors;
        }

        // static parse functions below
        /// <summary>
        /// Parse HTML into a Document. 
        /// </summary>
        /// <param name="html">HTML to parse</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>parsed Document</returns>
        public static Document Parse(string html, string baseUri)
        {
            HtmlTreeBuilder treeBuilder = new HtmlTreeBuilder();
            return treeBuilder.Parse(html, baseUri, ParseErrorList.NoTracking());
        }

        /// <summary>
        /// Parse a fragment of HTML into a list of nodes. The context element, if supplied, supplies parsing context.
        /// </summary>
        /// <param name="fragmentHtml">the fragment of HTML to parse</param>
        /// <param name="context">(optional) the element that this HTML fragment is being parsed for (i.e. for inner HTML). This provides stack context (for implicit element creation).</param>
        /// <param name="baseUri">base URI of document (i.e. original fetch location), for resolving relative URLs.</param>
        /// <returns>list of nodes parsed from the input HTML. Note that the context element, if supplied, is not modified.</returns>
        public static IList<Node> ParseFragment(string fragmentHtml, Element context, string baseUri)
        {
            HtmlTreeBuilder treeBuilder = new HtmlTreeBuilder();
            return treeBuilder.ParseFragment(fragmentHtml, context, baseUri, ParseErrorList.NoTracking());
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
        /// Utility method to unescape HTML entities from a string
        /// </summary>
        /// <param name="s">HTML escaped string</param>
        /// <param name="inAttribute">If the string is to be escaped in strict mode (as attributes are)</param>
        /// <returns>An unescaped string</returns>
        public static string UnescapeEntities(string s, bool inAttribute) {
            Tokeniser tokeniser = new Tokeniser(new CharacterReader(s), ParseErrorList.NoTracking());
            return tokeniser.UnescapeEntities(inAttribute);
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

        // builders

        /// <summary>
        /// Create a new HTML parser. This parser treats input as HTML5, and enforces the creation of a normalised document,
        /// based on a knowledge of the semantics of the incoming tags.
        /// </summary>
        /// <returns>A new HTML parser.</returns>
        public static Parser HtmlParser()
        {
            return new Parser(new HtmlTreeBuilder());
        }

        /// <summary>
        /// Create a new XML parser. This parser assumes no knowledge of the incoming tags and does not treat it as HTML,
        /// rather creates a simple tree directly from the input.
        /// </summary>
        /// <returns>A new simple XML parser.</returns>
        public static Parser XmlParser()
        {
            return new Parser(new XmlTreeBuilder());
        }
    }
}