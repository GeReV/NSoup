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
                return titleEl != null ? titleEl.Text.Trim() : string.Empty;
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
                    Head.AppendElement("title").SetText(value);
                }
                else
                {
                    titleEl.SetText(value);
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
                Body.AppendChild(new TextNode(" ", string.Empty));
                Body.AppendChild(node);
            }
        }

        public override string OuterHtml()
        {
            return base.OuterHtml();
        }

        /// <summary>
        /// Set the text of the <code>body</code> of this document. Any existing nodes within the body will be cleared.
        /// </summary>
        /// <param name="text">unencoded text</param>
        /// <returns>this document</returns>
        public override Element SetText(string text)
        {
            Body.SetText(text); // overridden to not nuke doc structure
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
    }
}
