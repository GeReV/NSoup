using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using NSoup.Select;

namespace NSoup.Nodes
{
    /// <summary>
    /// The base, abstract Node model. Elements, Documents, Comments etc are all Node instances.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public abstract class Node
    {
        private Node _parentNode;
        protected readonly List<Node> _childNodes;
        private readonly Attributes _attributes;
        private string _baseUri;
        private int _siblingIndex;

        /// <summary>
        /// Create a new Node.
        /// </summary>
        /// <param name="baseUri">base URI</param>
        /// <param name="attributes">attributes (not null, but may be empty)</param>
        protected Node(string baseUri, Attributes attributes)
        {
            if (baseUri == null)
            {
                throw new ArgumentNullException("baseUri");
            }
            if (attributes == null)
            {
                throw new ArgumentNullException("attributes");
            }

            _childNodes = new List<Node>();
            this._baseUri = baseUri.Trim();
            this._attributes = attributes;
        }

        protected Node(string baseUri)
            : this(baseUri, new Attributes())
        {
        }

        /// <summary>
        /// Gets the node name of this node. Use for debugging purposes and not logic switching (for that, use <code>is</code> keyword).
        /// </summary>
        public abstract string NodeName { get; }

        /// <summary>
        /// Get an attribute's value by its key.
        /// </summary>
        /// <remarks>
        /// To get an absolute URL from an attribute that may be a relative URL, prefix the key with <code><b>abs</b></code>, 
        /// which is a shortcut to the {@link #absUrl} method. 
        /// E.g.: <code>string url = a.attr("abs:href");</code>
        /// </remarks>
        /// <param name="attributeKey">The attribute key.</param>
        /// <returns>The attribute, or empty string if not present (to avoid nulls).</returns>
        /// <seealso cref="Attributes()"/>
        /// <seealso cref="HasAttr(string)"/>
        /// <seealso cref="AbsUrl(string)"/>
        public string Attr(string attributeKey)
        {
            if (attributeKey == null)
            {
                throw new ArgumentNullException("attributeKey");
            }

            if (HasAttr(attributeKey))
            {
                return _attributes.GetValue(attributeKey);
            }
            else if (attributeKey.ToLowerInvariant().StartsWith("abs:"))
            {
                return AbsUrl(attributeKey.Substring("abs:".Length));
            }
            else return string.Empty;
        }

        /// <summary>
        /// Gets all of the element's attributes.
        /// </summary>
        public Attributes Attributes
        {
            get { return _attributes; }
        }

        /// <summary>
        /// Set an attribute (key=value). If the attribute already exists, it is replaced.
        /// </summary>
        /// <param name="attributeKey">The attribute key.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns>this (for chaining)</returns>
        public Node Attr(string attributeKey, string attributeValue)
        {
            _attributes.Add(attributeKey, attributeValue);
            return this;
        }

        /// <summary>
        /// Test if this element has an attribute.
        /// </summary>
        /// <param name="attributeKey">The attribute key to check.</param>
        /// <returns>true if the attribute exists, false if not.</returns>
        public bool HasAttr(string attributeKey)
        {
            if (attributeKey == null)
            {
                throw new ArgumentNullException("attributeKey");
            }
            return _attributes.ContainsKey(attributeKey);
        }

        /// <summary>
        /// Remove an attribute from this element.
        /// </summary>
        /// <param name="attributeKey">The attribute to remove.</param>
        /// <returns>this (for chaining)</returns>
        public Node RemoveAttr(string attributeKey)
        {
            if (attributeKey == null)
            {
                throw new ArgumentNullException("attributeKey");
            }
            _attributes.Remove(attributeKey);
            return this;
        }

        /// <summary>
        /// Gets or sets the base URI of this node.
        /// </summary>
        public string BaseUri
        {
            get { return _baseUri; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                this._baseUri = value;
            }
        }

        /// <summary>
        /// Get an absolute URL from a URL attribute that may be relative (i.e. an <code>&lt;a href&gt;</code> or 
        /// <code>&lt;img src&gt;</code>.
        /// </summary>
        /// <remarks>
        /// If the attribute value is already absolute (i.e. it starts with a protocol, like 
        /// <code>http://</code> or <code>https://</code> etc), and it successfully parses as a URL, the attribute is 
        /// returned directly. Otherwise, it is treated as a URL relative to the element's <see cref="BaseUri"/>, and made 
        /// absolute using that. 
        /// As an alternate, you can use the <see cref="Attr()"/> method with the <code>abs:</code> prefix.
        /// </remarks>
        /// <param name="attributeKey">The attribute key</param>
        /// <returns>
        /// An absolute URL if one could be made, or an empty string (not null) if the attribute was missing or 
        /// could not be made successfully into a URL.
        /// </returns>
        /// <seealso cref="Attr()"/>
        /// <seealso cref="System.Uri"/>
        public string AbsUrl(string attributeKey)
        {
            if (string.IsNullOrEmpty(attributeKey))
            {
                throw new ArgumentNullException("attributeKey");
            }

            string relUrl = Attr(attributeKey);
            if (!HasAttr(attributeKey))
            {
                return string.Empty; // nothing to make absolute with
            }
            else
            {
                Uri baseUrl;
                try
                {
                    try
                    {
                        baseUrl = new Uri(_baseUri);
                    }
                    catch (UriFormatException)
                    {
                        // the base is unsuitable, but the attribute may be abs on its own, so try that
                        Uri abs = new Uri(relUrl);
                        return abs.ToString();
                    }
                    Uri abs2 = new Uri(baseUrl, relUrl);
                    return abs2.ToString();
                }
                catch (UriFormatException)
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Get a child node by index
        /// </summary>
        /// <param name="index">index of child node</param>
        /// <returns>the child node at this index.</returns>
        public Node GetChildNode(int index)
        {
            return _childNodes[index];
        }

        /// <summary>
        /// Gets this node's children. Presented as an unmodifiable list: new children can not be added, but the child nodes 
        /// themselves can be manipulated.
        /// </summary>
        public IList<Node> ChildNodes
        {
            get { return _childNodes.AsReadOnly(); }
        }

        public Node[] ChildNodesAsArray()
        {
            return ChildNodes.ToArray();
        }

        /// <summary>
        /// Gets or sets this node's parent node.
        /// </summary>
        public Node ParentNode
        {
            get { return _parentNode; }
            set
            {
                // TODO: Originally - protected SetParentNode(...).
                if (this._parentNode != null)
                {
                    this._parentNode.RemoveChild(this);
                }
                this._parentNode = value;
            }
        }

        /// <summary>
        /// Remove (delete) this node from the DOM tree. If this node has children, they are also removed.
        /// </summary>
        public void Remove()
        {
            if (ParentNode == null)
            {
                throw new InvalidOperationException("Parent node is null.");
            }

            ParentNode.RemoveChild(this);
        }
        
        /// <summary>
        /// Replace this node in the DOM with the supplied node.
        /// </summary>
        /// <param name="input">in the node that will will replace the existing node.</param>
        public void ReplaceWith(Node input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (ParentNode == null)
            {
                throw new InvalidOperationException("Parent node is null.");
            }

            ParentNode.ReplaceChild(this, input);
        }

        public void ReplaceChild(Node output, Node input)
        {
            if (output._parentNode != this)
            {
                throw new ArgumentException("Output's parent node must be equal to this object.");
            }
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (input._parentNode != null)
            {
                input._parentNode.RemoveChild(input);
            }

            int index = output.SiblingIndex;
            _childNodes[index] = input;
            input._parentNode = this;
            input.SiblingIndex = index;
            output._parentNode = null;
        }

        public void RemoveChild(Node output)
        {
            if (output._parentNode != this)
            {
                throw new ArgumentException("Output's parent node must be equal to this object.");
            }
            int index = output.SiblingIndex;
            _childNodes.RemoveAt(index);
            ReIndexChildren();
            output._parentNode = null;
        }

        public void AddChildren(params Node[] children)
        {
            AddChildren(ChildNodes.Count, children);
        }

        public void AddChildren(int index, params Node[] children)
        {
            if (children.Any(n => n == null))
            {
                throw new ArgumentException("`children` array cannot contain null objects.");
            }

            for (int i = children.Length - 1; i >= 0; i--)
            {
                Node input = children[i];
                if (input.ParentNode != null)
                {
                    input.ParentNode.RemoveChild(input);
                }

                ChildNodes.Insert(index, input);
                input.ParentNode = this;
            }
            ReIndexChildren();
        }

        private void ReIndexChildren()
        {
            for (int i = 0; i < _childNodes.Count; i++)
            {
                _childNodes[i].SiblingIndex = i;
            }
        }

        protected int NodeDepth
        {
            get
            {
                if (_parentNode == null)
                {
                    return 0;
                }
                else
                {
                    return _parentNode.NodeDepth + 1;
                }
            }
        }

        /// <summary>
        /// Retrieves this node's sibling nodes. Effectively, <see cref="ChildNodes"/>, node.Parent.ChildNodes.
        /// </summary>
        /// <returns>node siblings, including this node</returns>
        public IList<Node> SiblingNodes
        {
            get { return ParentNode.ChildNodes; } // TODO: should this strip out this node? i.e. not a sibling of self?
        }

        /// <summary>
        /// Gets this node's next sibling.
        /// </summary>
        public Node NextSibling
        {
            get
            {
                if (ParentNode == null)
                {
                    return null; // root
                }
                List<Node> siblings = _parentNode.ChildNodes.ToList();
                int index = SiblingIndex;
                //Validate.notNull(index);
                if (siblings.Count > index + 1)
                {
                    return siblings[index + 1];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets this node's previous sibling.
        /// </summary>
        public Node PreviousSibling
        {
            get
            {
                List<Node> siblings = _parentNode.ChildNodes.ToList();
                int index = SiblingIndex;
                if (index > 0)
                {
                    return siblings[index - 1];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the list index of this node in its node sibling list. I.e. if this is the first node sibling, returns 0.
        /// </summary>
        /// <seealso cref="Element.ElementSiblingIndex"/>
        public int SiblingIndex
        {
            get { return _siblingIndex; }
            set { this._siblingIndex = value; }
        }

        /// <summary>
        /// Get the outer HTML of this node.
        /// </summary>
        /// <returns>HTML</returns>
        public virtual string OuterHtml()
        {
            StringBuilder accum = new StringBuilder(32 * 1024);
            OuterHtml(accum);
            return accum.ToString();
        }

        public void OuterHtml(StringBuilder accum)
        {
            new NodeTraversor(new OuterHtmlVisitor(accum)).Traverse(this);
        }

        /// <summary>
        /// Gets the outer HTML of this node.
        /// </summary>
        /// <param name="accum">accumulator to place HTML into</param>
        public abstract void OuterHtmlHead(StringBuilder accum, int depth);

        public abstract void OuterHtmlTail(StringBuilder accum, int depth);

        public override string ToString()
        {
            return OuterHtml();
        }

        protected void Indent(StringBuilder accum, int depth)
        {
            accum.Append("\n").Append(string.Empty.PadLeft(depth));
        }

        public override bool Equals(object obj)
        {
            // todo: have nodes hold a child index, compare against that and parent (not children)
            return (this == obj);
        }

        public override int GetHashCode()
        {
            int result = ParentNode != null ? ParentNode.GetHashCode() : 0;
            // not children, or will block stack as they go back up to parent)
            result = 31 * result + (Attributes != null ? Attributes.GetHashCode() : 0);
            return result;
        }

        private class OuterHtmlVisitor : NodeVisitor
        {
            private StringBuilder accum;

            public OuterHtmlVisitor(StringBuilder accum)
            {
                this.accum = accum;
            }

            public void Head(Node node, int depth)
            {
                node.OuterHtmlHead(accum, depth);
            }

            public void Tail(Node node, int depth)
            {
                node.OuterHtmlTail(accum, depth);
            }
        }
    }
}
