using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using NSoup.Select;
using NSoup.Helper;
using NSoup.Parse;

namespace NSoup.Nodes
{
    /// <summary>
    /// The base, abstract Node model. Elements, Documents, Comments etc are all Node instances.
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public abstract class Node : ICloneable
    {
        private Node _parentNode;
        protected IList<Node> _childNodes;
        protected Attributes _attributes;
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

        protected Node()
        {
            _childNodes = new List<Node>();
            _attributes = null;
        }

        /// <summary>
        /// Gets the node name of this node. Use for debugging purposes and not logic switching (for that, use <code>is</code> keyword).
        /// </summary>
        public abstract string NodeName { get; }

        /// <summary>
        /// Gets all of the element's attributes.
        /// </summary>
        public virtual Attributes Attributes
        {
            get { return _attributes; }
            protected set { _attributes = value; }
        }

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
        public virtual string Attr(string attributeKey)
        {
            if (attributeKey == null)
            {
                throw new ArgumentNullException("attributeKey");
            }

            if (_attributes.ContainsKey(attributeKey))
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
        /// Set an attribute (key=value). If the attribute already exists, it is replaced.
        /// </summary>
        /// <param name="attributeKey">The attribute key.</param>
        /// <param name="attributeValue">The attribute value.</param>
        /// <returns>this (for chaining)</returns>
        public virtual Node Attr(string attributeKey, string attributeValue)
        {
            _attributes.Add(attributeKey, attributeValue);
            return this;
        }

        /// <summary>
        /// Test if this element has an attribute.
        /// </summary>
        /// <param name="attributeKey">The attribute key to check.</param>
        /// <returns>true if the attribute exists, false if not.</returns>
        public virtual bool HasAttr(string attributeKey)
        {
            if (attributeKey == null)
            {
                throw new ArgumentNullException("attributeKey");
            }

            if (attributeKey.ToLowerInvariant().StartsWith("abs:"))
            {
                string key = attributeKey.Substring("abs:".Length);
                if (_attributes.ContainsKey(key) && !AbsUrl(key).Equals(string.Empty))
                {
                    return true;
                }
            }

            return _attributes.ContainsKey(attributeKey);
        }

        /// <summary>
        /// Remove an attribute from this element.
        /// </summary>
        /// <param name="attributeKey">The attribute to remove.</param>
        /// <returns>this (for chaining)</returns>
        public virtual Node RemoveAttr(string attributeKey)
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
        /// <code>&lt;img src&gt;</code>).
        /// E.g.: <code>String absUrl = linkEl.absUrl("href");</code>
        /// </summary>
        /// <remarks>
        /// If the attribute value is already absolute (i.e. it starts with a protocol, like 
        /// <code>http://</code> or <code>https://</code> etc), and it successfully parses as a URL, the attribute is 
        /// returned directly. Otherwise, it is treated as a URL relative to the element's <see cref="BaseUri"/>, and made 
        /// absolute using that. 
        /// As an alternate, you can use the <see cref="Attr()"/> method with the <code>abs:</code> prefix, e.g.:
        /// <code>String absUrl = linkEl.attr("abs:href");</code>
        /// </remarks>
        /// <param name="attributeKey">The attribute key</param>
        /// <returns>
        /// An absolute URL if one could be made, or an empty string (not null) if the attribute was missing or 
        /// could not be made successfully into a URL.
        /// </returns>
        /// <seealso cref="Attr()"/>
        /// <seealso cref="System.Uri"/>
        public virtual string AbsUrl(string attributeKey)
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

                        // System.Uri will parse invalid schemes (for example, wtf://). An extra validation is added.
                        string[] schemes = new [] { 
                            Uri.UriSchemeFile, 
                            Uri.UriSchemeFtp, 
                            Uri.UriSchemeGopher, 
                            Uri.UriSchemeHttp, 
                            Uri.UriSchemeHttps, 
                            Uri.UriSchemeMailto, 
                            Uri.UriSchemeNetPipe, 
                            Uri.UriSchemeNetTcp, 
                            Uri.UriSchemeNews, 
                            Uri.UriSchemeNntp
                        };

                        if (!schemes.Contains(baseUrl.Scheme)) 
                        {
                            throw new UriFormatException("Invalid URI scheme.");
                        }
                    }
                    catch (UriFormatException)
                    {
                        // the base is unsuitable, but the attribute may be abs on its own, so try that
                        Uri abs = new Uri(relUrl);
                        return abs.ToString();
                    }

                    // workaround: java resolves '//path/file + ?foo' to '//path/?foo', not '//path/file?foo' as desired
                    if (relUrl.StartsWith("?"))
                    {
                        relUrl = baseUrl.AbsolutePath + relUrl;
                    }

                    Uri abs2 = new Uri(baseUrl, relUrl);
                    return abs2.ToString(); // Using the original string promises no tempering from the internals.
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
            get { return _childNodes; }
            private set { _childNodes = value; }
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
        /// Gets the Document associated with this Node. 
        /// </summary>
        /// <returns>the Document associated with this Node, or null if there is no such Document.</returns>
        public Document OwnerDocument
        {
            get
            {
                if (this is Document)
                {
                    return (Document)this;
                }
                else if (ParentNode == null)
                {
                    return null;
                }
                else
                {
                    return ParentNode.OwnerDocument;
                }
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
        /// Insert the specified HTML into the DOM before this node (i.e. as a preceeding sibling).
        /// </summary>
        /// <param name="html">HTML to add before this node</param>
        /// <returns>this node, for chaining</returns>
        /// <see cref="After(string)"/>
        public virtual Node Before(string html)
        {
            AddSiblingHtml(SiblingIndex, html);

            return this;
        }

        /// <summary>
        /// Insert the specified node into the DOM before this node (i.e. as a preceeding sibling).
        /// </summary>
        /// <param name="node">node to add before this node</param>
        /// <returns>this node, for chaining</returns>
        /// <see cref="After(Node)"/>
        public virtual Node Before(Node node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (ParentNode == null)
            {
                throw new InvalidOperationException("ParentNode is null.");
            }

            _parentNode.AddChildren(SiblingIndex, node);

            return this;
        }

        /// <summary>
        /// Insert the specified HTML into the DOM after this node (i.e. as a following sibling).
        /// </summary>
        /// <param name="html">HTML to add after this node</param>
        /// <returns>this node, for chaining</returns>
        /// <see cref="Before(string)"/>
        public virtual Node After(string html)
        {
            AddSiblingHtml(SiblingIndex + 1, html);

            return this;
        }

        /// <summary>
        /// Insert the specified node into the DOM after this node (i.e. as a following sibling).
        /// </summary>
        /// <param name="node">node to add after this node</param>
        /// <returns>this node, for chaining</returns>
        /// <see cref="Before(Node)"/>
        public virtual Node After(Node node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }
            if (ParentNode == null)
            {
                throw new InvalidOperationException("ParentNode is null.");
            }

            _parentNode.AddChildren(SiblingIndex + 1, node);

            return this;
        }

        private void AddSiblingHtml(int index, string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }

            if (ParentNode == null)
            {
                throw new NullReferenceException("ParentNode is null.");
            }

            Element context = ParentNode is Element ? (Element)ParentNode : null;
            IList<Node> nodes = Parser.ParseFragment(html, context, BaseUri);
            _parentNode.AddChildren(index, nodes.ToArray());
        }

        /// <summary>
        /// Wrap the supplied HTML around this node.
        /// </summary>
        /// <param name="html">HTML to wrap around this element, e.g. <code>&lt;div class="head"&gt;&lt;/div&gt;</code>. Can be arbitrarily deep.</param>
        /// <returns>this node, for chaining.</returns>
        public virtual Node Wrap(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentException("html cannot be empty.");
            }

            Element context = ParentNode is Element ? (Element)ParentNode : null;
            IList<Node> wrapChildren = Parser.ParseFragment(html, context, BaseUri);
            Node wrapNode = wrapChildren.First();
            if (wrapNode == null || !(wrapNode is Element))  // nothing to wrap with; noop
            {
                return null;
            }

            Element wrap = (Element)wrapNode;
            Element deepest = GetDeepChild(wrap);
            ParentNode.ReplaceChild(this, wrap);
            deepest.AddChildren(this);

            // remainder (unbalanced wrap, like <div></div><p></p> -- The <p> is remainder
            if (wrapChildren.Count > 0)
            {
                for (int i = 0; i < wrapChildren.Count; i++)
                { // skip first
                    Node remainder = wrapChildren[i];
                    remainder.ParentNode.RemoveChild(remainder);
                    wrap.AppendChild(remainder);
                }
            }

            return this;
        }

        /// <summary>
        /// Removes this node from the DOM, and moves its children up into the node's parent. This has the effect of dropping 
        /// the node but keeping its children.
        /// 
        /// For example, with the input html:
        /// <code>&lt;div&gt;One &lt;span&gt;Two &lt;b&gt;Three&lt;/b&gt;&lt;/span&gt;&lt;/div&gt;</code> 
        /// Calling <code>element.Unwrap()</code> on the <code>span</code> element will result in the html: 
        /// <code>&lt;div&gt;One Two &lt;b&gt;Three&lt;/b&gt;</code>
        /// and the <code>"Two "</code> <see cref="TextNode">TextNode</see> being returned.
        /// </summary>
        /// <returns>the first child of this node, after the node has been unwrapped. Null if the node had no children.</returns>
        /// <see cref="Remove()"/>
        /// <see cref="Wrap(string)"/>
        public Node Unwrap()
        {
            if (_parentNode == null)
            {
                throw new InvalidOperationException("Parent node is null.");
            }

            int index = SiblingIndex;

            Node firstChild = _childNodes.Count > 0 ? _childNodes[0] : null;

            _parentNode.AddChildren(index, this.ChildNodesAsArray());

            this.Remove();

            return firstChild;
        }

        private Element GetDeepChild(Element el)
        {
            List<Element> children = el.Children.ToList();
            if (children.Count > 0)
            {
                return GetDeepChild(children[0]);
            }
            else
            {
                return el;
            }
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
            //most used. short circuit addChildren(int), which hits reindex children and array copy
            foreach (Node child in children)
            {
                ReParentChild(child);
                ChildNodes.Add(child);
                child.SiblingIndex = ChildNodes.Count - 1;
            }
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

                ReParentChild(input);

                ChildNodes.Insert(index, input);
            }
            ReIndexChildren();
        }

        private void ReParentChild(Node child)
        {
            if (child.ParentNode != null)
            {
                child.ParentNode.RemoveChild(child);
            }
            child.ParentNode = this;
        }

        private void ReIndexChildren()
        {
            for (int i = 0; i < _childNodes.Count; i++)
            {
                _childNodes[i].SiblingIndex = i;
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
            new NodeTraversor(new OuterHtmlVisitor(accum, GetOutputSettings())).Traverse(this);
        }

        // if this node has no document (or parent), retrieve the default output settings
        private Document.OutputSettings GetOutputSettings()
        {
            return OwnerDocument != null ? OwnerDocument.GetOutputSettings() : (new Document(string.Empty)).GetOutputSettings();
        }

        /// <summary>
        /// Gets the outer HTML of this node.
        /// </summary>
        /// <param name="accum">accumulator to place HTML into</param>
        public abstract void OuterHtmlHead(StringBuilder accum, int depth, Document.OutputSettings output);

        public abstract void OuterHtmlTail(StringBuilder accum, int depth, Document.OutputSettings output);

        public override string ToString()
        {
            return OuterHtml();
        }

        protected void Indent(StringBuilder accum, int depth, Document.OutputSettings output)
        {
            accum.Append("\n").Append(StringUtil.Padding(depth * output.IndentAmount()));
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

        #region ICloneable Members

        /// <summary>
        /// Create a stand-alone, deep copy of this node, and all of its children. The cloned node will have no siblings or
        /// parent node. As a stand-alone object, any changes made to the clone or any of its children will not impact the
        /// original node.
        /// </summary>
        /// <remarks>
        /// The cloned node may be adopted into another Document or node structure using <see cref="Element.AppendChild(Node)"/>.
        /// </remarks>
        /// <returns>stand-alone cloned node</returns>
        public object Clone()
        {
            return DoClone(null);
        }

        protected Node DoClone(Node parent) {
            Node clone;
            try
            {
                // Originally: (Node)super.clone();
                // Base class of Node is System.Object, therefore the following is needed:
                clone = (Node)this.MemberwiseClone();
            }
            catch (Exception)
            {
                throw;
            }

            // Note: Original Java code accesses actual members, not accessors.

            clone._parentNode = parent; // can be null, to create an orphan split
            clone._siblingIndex = parent == null ? 0 : SiblingIndex;
            clone._attributes = Attributes != null ? (Attributes)Attributes.Clone() : null;
            clone._baseUri = _baseUri;
            clone._childNodes = new List<Node>(_childNodes.Count);

            // We have to use for, instead of foreach, since .NET does not allow addition of items inside foreach.
            for (int i = 0; i < _childNodes.Count; i++)
            {
                Node child = _childNodes[i];

                clone._childNodes.Add(child.DoClone(clone)); // clone() creates orphans, doClone() keeps parent
            }

            return clone;
        }

        #endregion

        private class OuterHtmlVisitor : NodeVisitor
        {
            private StringBuilder _accum;
            private Document.OutputSettings _output;

            public OuterHtmlVisitor(StringBuilder accum, Document.OutputSettings output)
            {
                this._accum = accum;
                this._output = output;
            }

            public void Head(Node node, int depth)
            {
                node.OuterHtmlHead(_accum, depth, _output);
            }

            public void Tail(Node node, int depth)
            {
                if (!node.NodeName.Equals("#text")) // saves a void hit.
                {
                    node.OuterHtmlTail(_accum, depth, _output);
                }
            }
        }
    }
}
