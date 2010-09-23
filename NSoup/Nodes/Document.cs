using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Parse;

namespace NSoup.Nodes
{
    /// <summary>
    /// A HTML Document.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Document : Element
    {
        private OutputSettings outputSettings = new OutputSettings();

        /// <summary>
        /// Create a new, empty Document.

        /// </summary>
        /// <param name="baseUri">base URI of document</param>
        /// <see cref="NSoup.Parse()" />
        /// <see cref="CreateShell(string)"/>
        public Document(string baseUri)
            : base(Tag.ValueOf("#root"), baseUri)
        {
        }

        /// <summary>
        /// Create a valid, empty shell of a document, suitable for adding more elements to.
        /// </summary>
        /// <param name="baseUri">baseUri of document</param>
        /// <returns>document with html, head, and body elements.</returns>
        static public Document CreateShell(string baseUri)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri");
            }

            Document doc = new Document(baseUri);
            Element html = doc.AppendElement("html");
            html.AppendElement("head");
            html.AppendElement("body");

            return doc;
        }

        /// <summary>
        /// Gets the document's <code>head</code> element.
        /// </summary>
        public Element Head
        {
            get { return GetElementsByTag("head").First; }
        }

        /// <summary>
        /// Gets the document's <code>body</code> element.
        /// </summary>
        public Element Body
        {
            get { return GetElementsByTag("body").First; }
        }

        /// <summary>
        /// Gets or sets the string contents of the document's {@code title} element.
        /// On set, updates the existing element, or adds {@code title} to {@code head} if
        /// not present.
        /// </summary>
        public string Title
        {
            get
            {
                Element titleEl = GetElementsByTag("title").First;
                return titleEl != null ? titleEl.Text().Trim() : string.Empty;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                Element titleEl = GetElementsByTag("title").First;
                if (titleEl == null)
                { // add to head
                    Head.AppendElement("title").Text(value);
                }
                else
                {
                    titleEl.Text(value);
                }
            }
        }

        /// <summary>
        /// Create a new Element, with this document's base uri. Does not make the new element a child of this document.
        /// </summary>
        /// <param name="tagName">element tag name (e.g. <code>a</code>)</param>
        /// <returns>new Element</returns>
        public Element CreateElement(string tagName)
        {
            return new Element(Tag.ValueOf(tagName), this.BaseUri);
        }

        /// <summary>
        /// Normalise the document. This happens after the parse phase so generally does not need to be called.
        /// Moves any text content that is not in the body element into the body.
        /// </summary>
        /// <returns>this document after normalisation</returns>
        public Document Normalise()
        {
            if (Select("html").IsEmpty)
                AppendElement("html");
            if (Head == null)
                Select("html").First.PrependElement("head");
            if (Body == null)
                Select("html").First.AppendElement("body");

            // pull text nodes out of root, html, and head els, and push into body. non-text nodes are already taken care
            // of. do in inverse order to maintain text order.
            Normalise(Head);
            Normalise(Select("html").First);
            Normalise(this);

            return this;
        }

        // does not recurse.
        private void Normalise(Element element)
        {
            List<Node> toMove = new List<Node>();
            foreach (Node node in element.ChildNodes)
            {
                if (node is TextNode)
                {
                    TextNode tn = (TextNode)node;
                    if (!tn.IsBlank)
                    {
                        toMove.Add(tn);
                    }
                }
            }

            for (int i = toMove.Count - 1; i >= 0; i--)
            {
                Node node = toMove[i];
                element.RemoveChild(node);
                Body.PrependChild(new TextNode(" ", string.Empty));
                Body.PrependChild(node);
            }
        }

        public override string OuterHtml()
        {
            return base.Html();
        }

        /// <summary>
        /// Set the text of the <code>body</code> of this document. Any existing nodes within the body will be cleared.
        /// </summary>
        /// <param name="text">unencoded text</param>
        /// <returns>this document</returns>
        public override Element Text(string text)
        {
            Body.Text(text); // overridden to not nuke doc structure
            return this;
        }

        /// <summary>
        /// Gets the node's name.
        /// </summary>
        public override string NodeName
        {
            get
            {
                return "#document";
            }
        }

        /**
     * A Document's output settings control the form of the text() and html() methods.
     */
        public class OutputSettings
        {
            private Entities.EscapeMode _escapeMode = Entities.EscapeMode.Base;
            private Encoding _encoding = Encoding.UTF8;
            private Encoder _encoder = null;

            public OutputSettings()
            {
                _encoder = _encoding.GetEncoder();
            }

            /// <summary>
            /// Gets or sets the document's current HTML escape mode: <code>base</code>, which provides a limited set of named HTML 
            /// entities and escapes other characters as numbered entities for maximum compatibility; or <code>extended</code>, 
            /// which uses the complete set of HTML named entities. 
            /// <p> 
            /// The default escape mode is <code>base</code>. 
            /// </summary>
            public Entities.EscapeMode EscapeMode
            {
                get { return _escapeMode; }
                set { this._escapeMode = value; }
            }

            /// <summary>
            /// Set the document's escape mode
            /// </summary>
            /// <param name="escapeMode">the new escape mode to use</param>
            /// <returns>the document's output settings, for chaining</returns>
            public OutputSettings SetEscapeMode(Entities.EscapeMode escapeMode)
            {
                this._escapeMode = escapeMode;
                return this;
            }

            /// <summary>
            /// Gets or sets the document's current output charset, which is used to control which characters are escaped when 
            /// generating HTML (via the <code>html()</code> methods), and which are kept intact. 
            /// <p> 
            /// Where possible (when parsing from a URL or File), the document's output charset is automatically set to the 
            /// input charset. Otherwise, it defaults to UTF-8.
            /// </summary>
            public Encoding Encoding
            {
                get { return _encoding; }
                set
                {
                    this._encoding = value;
                    this._encoder = value.GetEncoder();
                }
            }

            /// <summary>
            /// Update the document's output charset.
            /// </summary>
            /// <param name="encoding">the new encoding to use.</param>
            /// <returns>the document's output settings, for chaining</returns>
            public OutputSettings SetEncoding(Encoding encoding)
            {
                // todo: this should probably update the doc's meta charset
                this.Encoding = encoding;
                return this;
            }

            /// <summary>
            /// Update the document's output charset.
            /// </summary>
            /// <param name="encoding">the new charset (by name) to use.</param>
            /// <returns>the document's output settings, for chaining</returns>
            public OutputSettings SetEncoding(string encoding)
            {
                SetEncoding(Encoding.GetEncoding(encoding));
                return this;
            }

            public Encoder Encoder
            {
                get { return _encoder; }
            }
        }

        /// <summary>
        /// Gets the document's current output settings.
        /// </summary>
        /// <remarks>Changed to "Settings" due to ambiguity between property and class.</remarks>
        public OutputSettings Settings
        {
            get { return outputSettings; }
        }
    }
}
