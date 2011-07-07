using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Helper;

namespace NSoup.Nodes
{
    /// <summary>
    /// A <code>&lt;!DOCTPYE&gt;</code> node.
    /// </summary>
    public class DocumentType : Node
    {
        // todo: quirk mode from publicId and systemId

        /// <summary>
        /// Create a new doctype element.
        /// </summary>
        /// <param name="name">the doctype's name</param>
        /// <param name="publicId">the doctype's public ID</param>
        /// <param name="systemId">the doctype's system ID</param>
        /// <param name="baseUri">the doctype's base URI</param>
        public DocumentType(string name, string publicId, string systemId, string baseUri)
            : base(baseUri)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            Attr("name", name);
            Attr("publicId", publicId);
            Attr("systemId", systemId);
        }

        public override string NodeName
        {
            get { return "#doctype"; }
        }

        public override void OuterHtmlHead(StringBuilder accum, int depth, Document.OutputSettings output)
        {
            accum.Append("<!DOCTYPE ").Append(Attr("name"));
            
            if (!StringUtil.IsBlank(Attr("publicId")))
            {
                accum.Append(" PUBLIC \"").Append(Attr("publicId")).Append("\"");
            }
            
            if (!StringUtil.IsBlank(Attr("systemId")))
            {
                accum.Append(" \"").Append(Attr("systemId")).Append("\"");
            }

            accum.Append('>');
        }

        public override void OuterHtmlTail(StringBuilder accum, int depth, Document.OutputSettings output)
        {
        }
    }
}
