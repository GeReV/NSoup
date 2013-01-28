using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;
using NSoup.Parse;

namespace NSoup.Safety
{
    /// <summary>
    /// The whitelist based HTML cleaner. Use to ensure that end-user provided HTML contains only the elements and attributes 
    /// that you are expecting; no junk, and no cross-site scripting attacks!
    /// </summary>
    /// <remarks>
    /// The HTML cleaner parses the input as HTML and then runs it through a white-list, so the output HTML can only contain 
    /// HTML that is allowed by the whitelist.
    /// It is assumed that the input HTML is a body fragment; the clean methods only pull from the source's body, and the 
    /// canned white-lists only allow body contained tags.
    /// Rather than interacting directly with a Cleaner object, generally see the <code>Clean</code> methods in <see cref="NSoup"/>.
    /// </remarks>
    public class Cleaner
    {
        private Whitelist _whitelist;

        /// <summary>
        /// Create a new cleaner, that sanitizes documents using the supplied whitelist.
        /// </summary>
        /// <param name="whitelist">white-list to clean with</param>
        public Cleaner(Whitelist whitelist)
        {
            if (whitelist == null)
            {
                throw new ArgumentNullException("whitelist");
            }
            this._whitelist = whitelist;
        }

        /// <summary>
        /// Creates a new, clean document, from the original dirty document, containing only elements allowed by the whitelist. 
        /// The original document is not modified. Only elements from the dirt document's <code>body</code> are used.
        /// </summary>
        /// <param name="dirtyDocument">Untrusted base document to clean.</param>
        /// <returns>cleaned document.</returns>
        public Document Clean(Document dirtyDocument)
        {
            if (dirtyDocument == null)
            {
                throw new ArgumentNullException("dirtyDocument");
            }

            Document clean = Document.CreateShell(dirtyDocument.BaseUri);
            if (dirtyDocument.Body != null) // frameset documents won't have a body. the clean doc will have empty body.
            {
                CopySafeNodes(dirtyDocument.Body, clean.Body);
            }

            return clean;
        }

        /// <summary>
        /// Determines if the input document is valid, against the whitelist. It is considered valid if all the tags and attributes 
        /// in the input HTML are allowed by the whitelist.
        /// </summary>
        /// <remarks>
        /// This method can be used as a validator for user input forms. An invalid document will still be cleaned successfully 
        /// using the <see cref="Clean(Document)"/> document. If using as a validator, it is recommended to still clean the document 
        /// to ensure enforced attributes are set correctly, and that the output is tidied.
        /// </remarks>
        /// <param name="dirtyDocument">document to test</param>
        /// <returns>true if no tags or attributes need to be removed; false if they do</returns>
        public bool IsValid(Document dirtyDocument)
        {
            if (dirtyDocument == null)
            {
                throw new ArgumentNullException("dirtyDocument");
            }

            Document clean = Document.CreateShell(dirtyDocument.BaseUri);
            int numDiscarded = CopySafeNodes(dirtyDocument.Body, clean.Body);
            return numDiscarded == 0;
        }

        /// <summary>
        /// Iterates the input and copies trusted nodes (tags, attributes, text) into the destination.
        /// </summary>
        /// <param name="source">source of HTML</param>
        /// <param name="dest">destination element to copy into</param>
        /// <returns>number of discarded elements (that were considered unsafe)</returns>
        private int CopySafeNodes(Element source, Element dest)
        {
            IList<Node> sourceChildren = source.ChildNodes;
            int numDiscarded = 0;

            foreach (Node sourceChild in sourceChildren)
            {
                if (sourceChild is Element)
                {
                    Element sourceEl = (Element)sourceChild;

                    if (_whitelist.IsSafeTag(sourceEl.TagName()))
                    { // safe, clone and copy safe attrs
                        ElementMeta meta = CreateSafeElement(sourceEl);
                        Element destChild = meta.Element;
                        dest.AppendChild(destChild);

                        numDiscarded += meta.NumAttributesDiscarded;
                        numDiscarded += CopySafeNodes(sourceEl, destChild); // recurs
                    }
                    else
                    { // not a safe tag, but it may have children (els or text) that are, so recurse
                        numDiscarded++;
                        numDiscarded += CopySafeNodes(sourceEl, dest);
                    }
                }
                else if (sourceChild is TextNode)
                {
                    TextNode sourceText = (TextNode)sourceChild;
                    TextNode destText = new TextNode(sourceText.GetWholeText(), sourceChild.BaseUri);
                    dest.AppendChild(destText);
                } // else, we don't care about comments, xml proc instructions, etc
            }
            return numDiscarded;
        }

        private ElementMeta CreateSafeElement(Element sourceEl)
        {
            string sourceTag = sourceEl.TagName();
            Attributes destAttrs = new Attributes();
            Element dest = new Element(Tag.ValueOf(sourceTag), sourceEl.BaseUri, destAttrs);
            int numDiscarded = 0;

            Attributes sourceAttrs = sourceEl.Attributes;
            foreach (NSoup.Nodes.Attribute sourceAttr in sourceAttrs)
            {
                if (_whitelist.IsSafeAttribute(sourceTag, sourceEl, sourceAttr))
                    destAttrs.Add(sourceAttr);
                else
                    numDiscarded++;
            }
            Attributes enforcedAttrs = _whitelist.GetEnforcedAttributes(sourceTag);

            foreach (NSoup.Nodes.Attribute item in enforcedAttrs)
            {
                destAttrs.Add(item);
            }

            return new ElementMeta(dest, numDiscarded);
        }

        private class ElementMeta
        {
            public Element Element { get; private set; }
            public int NumAttributesDiscarded { get; private set; }

            public ElementMeta(Element el, int numAttribsDiscarded)
            {
                this.Element = el;
                this.NumAttributesDiscarded = numAttribsDiscarded;
            }
        }

    }
}