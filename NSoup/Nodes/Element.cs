using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Parse;
using NSoup.Select;
using System.Text.RegularExpressions;

namespace NSoup.Nodes
{
    
    /// <summary>
    /// A HTML element consists of a tag name, attributes, and child nodes (including text nodes and other elements).
    /// </summary>
    /// <remarks>From an Element, you can extract data, traverse the node graph, and manipulate the HTML.</remarks>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Element : Node
    {
        private readonly Tag _tag;
        private HashSet<string> _classNames; // TODO: Originally: Set<string>.

        /// <summary>
        /// Create a new, standalone Element. (Standalone in that is has no parent.)
        /// </summary>
        /// <param name="tag">tag of this element</param>
        /// <param name="baseUri">baseUri the base URI</param>
        /// <param name="attributes">initial attributes</param>
        /// <see cref="AppendChild(Node)"/>
        /// <see cref="AppendElement(string)"/>
        public Element(Tag tag, string baseUri, Attributes attributes)
            : base(baseUri, attributes)
        {

            if (tag == null)
            {
                throw new ArgumentNullException("tag");
            }
            this._tag = tag;
        }

        /// <summary>
        /// Create a new Element from a tag and a base URI.
        /// </summary>
        /// <param name="tag">element tag</param>
        /// <param name="baseUri">the base URI of this element. It is acceptable for the base URI to be an empty string, but not null.</param>
        /// <see cref="Tag.ValueOf(string)"/>
        public Element(Tag tag, string baseUri)
            : this(tag, baseUri, new Attributes())
        {
        }

        /// <summary>
        /// Gets the node's name.
        /// </summary>
        public override string NodeName
        {
            get { return _tag.Name; }
        }

        /// <summary>
        /// Gets the name of the tag for this element. E.g: <code>div</code>
        /// </summary>
        public string TagName
        {
            get { return _tag.Name; }
        }

        /// <summary>
        /// Gets the Tag for this element.
        /// </summary>
        public Tag Tag
        {
            get { return _tag; }
        }

        /// <summary>
        /// Test if this element is a block-level element. (E.g. <code>&lt;div&gt; == true</code> or an inline element <code>&lt;p&gt; == false</code>).
        /// </summary>
        public bool IsBlock
        {
            get { return _tag.IsBlock; }
        }

        /// <summary>
        /// Gets the <code>id</code> attribute of this element.
        /// </summary>
        public string Id
        {
            get
            {
                string id = Attr("id");
                return id == null ? string.Empty : id;
            }
        }

        /// <summary>
        /// Set an attribute value on this element. If this element already has an attribute with the 
        /// key, its value is updated; otherwise, a new attribute is added.
        /// </summary>
        /// <param name="attributeKey">attribute key</param>
        /// <param name="attributeValue">attribute value</param>
        /// <returns>this element</returns>
        public new Element Attr(string attributeKey, string attributeValue)
        {
            base.Attr(attributeKey, attributeValue);
            return this;
        }

        /// <summary>
        /// Gets the parent element.
        /// </summary>
        public Element Parent
        {
            get { return (Element)base.ParentNode; }
        }

        /// <summary>
        /// Gets this element's parent and ancestors, up to the document root.
        /// </summary>
        public Elements Parents
        {
            get
            {
                Elements parents = new Elements();
                AccumulateParents(this, parents);
                return parents;
            }
        }

        private static void AccumulateParents(Element el, Elements parents)
        {
            Element parent = el.Parent;
            if (parent != null && !parent.TagName.Equals("#root"))
            {
                parents.Add(parent);
                AccumulateParents(parent, parents);
            }
        }

        /// <summary>
        /// Get a child element of this element, by its 0-based index number.
        /// 
        /// * @param index the index number of the element to retrieve
        /// </summary>
        /// <remarks>
        /// Note that an element can have both mixed Nodes and Elements as children. This method inspects 
        /// a filtered list of children that are elements, and the index is based on that filtered list.
        /// </remarks>
        /// <param name="index">The index of child to return</param>
        /// <returns>the child element, if it exists, or <code>null</code> if absent.</returns>
        /// <see cref="ChildNode(int)"/>
        public Element Child(int index)
        {
            return Children[index];
        }

        /// <summary>
        /// Gets this element's child elements.
        /// </summary>
        /// <remarks>
        /// This is effectively a filter on {@link #childNodes()} to get Element nodes.
        /// If this element has no children, returns an empty list.
        /// </remarks>
        /// <see cref="ChildNodes()"/>
        public Elements Children
        {
            get
            {
                // create on the fly rather than maintaining two lists. if gets slow, memoize, and mark dirty on change
                List<Element> elements = new List<Element>();
                foreach (Node node in ChildNodes)
                {
                    if (node is Element)
                    {
                        elements.Add((Element)node);
                    }
                }
                return new Elements(elements);
            }
        }

        /// <summary>
        /// Find elements that match the selector query, with this element as the starting context. Matched elements 
        /// may include this element, or any of its children.
        /// </summary>
        /// <param name="query">a Selector query</param>
        /// <returns>elements that match the query (empty if none match)</returns>
        /// <see cref="NSoup.Select.Selector"/>
        /// <remarks>
        /// This method is generally more powerful to use than the DOM-type {@code getElementBy*} methods, because 
        /// multiple filters can be combined, e.g.: 
        /// &lt;ul&gt;
        /// &lt;li&gt;<code>el.select("a[href]")</code> - finds links <code>a</code> tags with <code>href</code> attributes) 
        /// &lt;li&gt;<code>el.select("a[href*=example.com]")</code> - finds links pointing to example.com (loosely) 
        /// &lt;/ul&gt; 
        /// See the query syntax documentation in <see cref="NSoup.Select.Selector"/>
        /// </remarks>
        public Elements Select(string query)
        {
            return Selector.Select(query, this);
        }

        /// <summary>
        /// Add a node to the last child of this element.
        /// </summary>
        /// <param name="child">node to add. Must not already have a parent.</param>
        /// <returns>this element, so that you can add more child nodes or elements.</returns>
        public Element AppendChild(Node child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            AddChildren(child);

            return this;
        }

        /// <summary>
        /// Add a node to the start of this element's children.
        /// </summary>
        /// <param name="child">node to add. Must not already have a parent.</param>
        /// <returns>this element, so that you can add more child nodes or elements.</returns>
        public Element PrependChild(Node child)
        {
            if (child == null)
            {
                throw new ArgumentNullException("child");
            }

            AddChildren(0, child);

            return this;
        }

        /// <summary>
        /// Create a new element by tag name, and add it as the last child.
        /// </summary>
        /// <param name="tagName">the name of the tag (e.g. <code>div</code>).</param>
        /// <returns>the new element, to allow you to add content to it, e.g.:
        /// <code>parent.AppendElement("h1").Attr("id", "header").SetText("Welcome");</code>
        /// </returns>
        public Element AppendElement(string tagName)
        {
            Element child = new Element(Tag.ValueOf(tagName), BaseUri);
            AppendChild(child);
            return child;
        }

        /// <summary>
        /// Create a new element by tag name, and add it as the first child.
        /// </summary>
        /// <param name="tagName">the name of the tag (e.g. <code>div</code>).</param>
        /// <returns>
        /// the new element, to allow you to add content to it, e.g.: 
        /// <code>parent.PrependElement("h1").Attr("id", "header").SetText("Welcome");</code>
        /// </returns>
        public Element PrependElement(string tagName)
        {
            Element child = new Element(Tag.ValueOf(tagName), BaseUri);
            PrependChild(child);
            return child;
        }

        /// <summary>
        /// Create and append a new TextNode to this element.
        /// </summary>
        /// <param name="text"the unencoded text to add></param>
        /// <returns>this element</returns>
        public Element AppendText(string text)
        {
            TextNode node = new TextNode(text, BaseUri);
            AppendChild(node);
            return this;
        }

        /// <summary>
        /// Create and prepend a new TextNode to this element.
        /// </summary>
        /// <param name="text">the unencoded text to add</param>
        /// <returns>this element</returns>
        public Element PrependText(string text)
        {
            TextNode node = new TextNode(text, BaseUri);
            PrependChild(node);
            return this;
        }

        /// <summary>
        /// Add inner HTML to this element. The supplied HTML will be parsed, and each node appended to the end of the children.
        /// </summary>
        /// <param name="html">HTML to add inside this element, after the existing HTML</param>
        /// <returns>this element</returns>
        /// <see cref="Html(string)"/>
        public Element Append(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }

            Element fragment = Parser.ParseBodyFragmentRelaxed(html, BaseUri).Body;
            AddChildren(fragment.ChildNodesAsArray());

            return this;
        }

        /// <summary>
        /// Add inner HTML into this element. The supplied HTML will be parsed, and each node prepended to the start of the children.
        /// </summary>
        /// <param name="html">HTML to add inside this element, before the existing HTML</param>
        /// <returns>this element</returns>
        /// <see cref="Html(string)"/>
        public Element Prepend(string html)
        {
            if (html == null)
            {
                throw new ArgumentNullException("html");
            }

            Element fragment = Parser.ParseBodyFragmentRelaxed(html, BaseUri).Body;
            AddChildren(0, fragment.ChildNodesAsArray());
            return this;
        }

        /// <summary>
        /// Insert the specified HTML into the DOM before this element (i.e. as a preceeding sibling).
        /// </summary>
        /// <param name="html">HTML to add before this element</param>
        /// <returns>this element, for chaining</returns>
        /// <seealso cref="After(string)"/>
        public Element Before(string html)
        {
            AddSiblingHtml(SiblingIndex, html);
            return this;
        }

        /// <summary>
        /// Insert the specified HTML into the DOM after this element (i.e. as a following sibling).
        /// </summary>
        /// <param name="html">HTML to add after this element</param>
        /// <returns>this element, for chaining</returns>
        /// <seealso cref="Before(string)"/>
        public Element After(string html)
        {
            AddSiblingHtml(SiblingIndex + 1, html);
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
                throw new Exception("ParentNode is null");
            }

            Element fragment = Parser.ParseBodyFragmentRelaxed(html, BaseUri).Body;
            ParentNode.AddChildren(index, fragment.ChildNodesAsArray());
        }

        /// <summary>
        /// Remove all of the element's child nodes. Any attributes are left as-is.
        /// </summary>
        /// <returns>this element</returns>
        public Element Empty()
        {
            _childNodes.Clear();
            return this;
        }

        /// <summary>
        /// Wrap the supplied HTML around this element.
        /// </summary>
        /// <param name="html">HTML to wrap around this element, e.g. <code>&lt;div class="head"&gt;&lt;/div&gt;</code>. Can be arbitralily deep.</param>
        /// <returns>this element, for chaining.</returns>
        public Element Wrap(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentNullException("html");
            }

            Element wrapBody = NSoup.Parse.Parser.ParseBodyFragmentRelaxed(html, BaseUri).Body;
            Elements wrapChildren = wrapBody.Children;
            Element wrap = wrapChildren.First;
            if (wrap == null)
            { // nothing to wrap with; noop
                return null;
            }

            Element deepest = GetDeepChild(wrap);
            ParentNode.ReplaceChild(this, wrap);
            deepest.AddChildren(this);

            // remainder (unbalananced wrap, like <div></div><p></p> -- The <p> is remainder
            if (wrapChildren.Count > 1)
            {
                for (int i = 1; i < wrapChildren.Count; i++)
                { // skip first
                    Element remainder = wrapChildren[i];
                    remainder.ParentNode.RemoveChild(remainder);
                    wrap.AppendChild(remainder);
                }
            }
            return this;
        }

        private Element GetDeepChild(Element el)
        {
            IList<Element> children = el.Children;
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
        /// Gets sibling elements.
        /// </summary>
        public Elements SiblingElements
        {
            get { return Parent.Children; }
        }

        /// <summary>
        /// Gets the next sibling element of this element. E.g., if a <code>div</code> contains two <code>p</code>s, 
        /// the <code>NextElementSibling</code> of the first <code>p</code> is the second <code>p</code>.
        /// </summary>
        /// <remarks>
        /// This is similar to {@link #nextSibling()}, but specifically finds only Elements.
        /// </remarks>
        /// <see cref="PreviousElementSibling()"/>
        public Element NextElementSibling
        {
            get
            {
                IList<Element> siblings = Parent.Children;
                int index = siblings.IndexOf(this);

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
        /// Gets the previous element sibling of this element.
        /// </summary>
        /// <see cref="NextElementSibling()"/>
        public Element PreviousElementSibling
        {
            get
            {
                IList<Element> siblings = Parent.Children;
                int index = siblings.IndexOf(this);

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
        /// Gets the first element sibling of this element.
        /// Will return the first sibling that is an element (aka the parent's first element child).
        /// </summary>
        public Element FirstElementSibling
        {
            get
            {
                // todo: should firstSibling() exclude this?
                IList<Element> siblings = Parent.Children;
                return siblings.Count > 1 ? siblings[0] : null;
            }
        }

        /// <summary>
        /// Gets the list index of this element in its element sibling list. I.e. if this is the first element 
        /// sibling, returns 0.
        /// </summary>
        public int ElementSiblingIndex
        {
            get
            {
                if (Parent == null)
                {
                    return 0;
                }

                return Parent.Children.IndexOf(this);
            }
        }

        /// <summary>
        /// Gets the last element sibling of this element.
        /// Will return the last sibling that is an element (aka the parent's last element child).
        /// </summary>
        public Element LastElementSibling
        {
            get
            {
                IList<Element> siblings = Parent.Children;
                return siblings.Count > 1 ? siblings[siblings.Count - 1] : null;
            }
        }

        // DOM type methods

        /// <summary>
        /// Finds elements, including and recursively under this element, with the specified tag name.
        /// </summary>
        /// <param name="tagName">The tag name to search for (case insensitively).</param>
        /// <returns>a matching unmodifiable list of elements. Will be empty if this element and none of its children match.</returns>
        public Elements GetElementsByTag(string tagName)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                throw new ArgumentNullException("tagName");
            }
            tagName = tagName.ToLowerInvariant().Trim();

            return Collector.Collect(new Evaluator.Tag(tagName), this);
        }

        /// <summary>
        /// Find an element by ID, including or under this element.
        /// </summary>
        /// <param name="id">The ID to search for.</param>
        /// <returns>The first matching element by ID, starting with this element, or null if none found.</returns>
        /// <remarks>
        /// Note that this finds the first matching ID, starting with this element. If you search down from a different 
        /// starting point, it is possible to find a different element by ID. For unique element by ID within a Document, 
        /// use <seealso cref="Document.GetElementById(string)"/>
        /// </remarks>
        public Element GetElementById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            Elements elements = Collector.Collect(new Evaluator.Id(id), this);
            if (elements.Count > 0)
            {
                return elements[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Find elements that have this class, including or under this element. Case insensitive.
        /// </summary>
        /// <param name="className">className the name of the class to search for.</param>
        /// <returns>elements with the supplied class name, empty if none</returns>
        /// <remarks>
        /// Elements can have multiple classes (e.g. {@code <div class="header round first">}. This method 
        /// checks each class, so you can find the above with {@code el.getElementsByClass("header");}.
        /// </remarks>
        /// <seealso cref="HasClass(string)"/>
        /// <seealso cref="ClassNames()"/>
        public Elements GetElementsByClass(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException("className");
            }

            return Collector.Collect(new Evaluator.Class(className), this);
        }

        /// <summary>
        /// Find elements that have a named attribute set. Case insensitive.
        /// </summary>
        /// <param name="key">name of the attribute</param>
        /// <returns>elements that have this attribute, empty if none</returns>
        public Elements GetElementsByAttribute(string key)
        {
            key = key.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            return Collector.Collect(new Evaluator.Attribute(key), this);
        }

        /// <summary>
        /// Find elements that have an attribute with the specific value. Case insensitive.
        /// </summary>
        /// <param name="key">name of the attribute</param>
        /// <param name="value">value of the attribute</param>
        /// <returns>elements that have this attribute with this value, empty if none</returns>
        public Elements GetElementsByAttributeValue(string key, string value)
        {
            return Collector.Collect(new Evaluator.AttributeWithValue(key, value), this);
        }

        /// <summary>
        /// Find elements that either do not have this attribute, or have it with a different value. Case insensitive.
        /// </summary>
        /// <param name="key">name of the attribute</param>
        /// <param name="value">value of the attribute</param>
        /// <returns>elements that do not have a matching attribute</returns>
        public Elements GetElementsByAttributeValueNot(string key, string value)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueNot(key, value), this);
        }

        /// <summary>
        /// Find elements that have attributes that start with the value prefix. Case insensitive.
        /// </summary>
        /// <param name="key">name of the attribute</param>
        /// <param name="valuePrefix">start of attribute value</param>
        /// <returns>elements that have attributes that start with the value prefix</returns>
        public Elements GetElementsByAttributeValueStarting(string key, string valuePrefix)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueStarting(key, valuePrefix), this);
        }

        /// <summary>
        /// Find elements that have attributes that end with the value suffix. Case insensitive.
        /// </summary>
        /// <param name="key">name of the attribute</param>
        /// <param name="valueSuffix">end of the attribute value</param>
        /// <returns>elements that have attributes that end with the value suffix</returns>
        public Elements GetElementsByAttributeValueEnding(string key, string valueSuffix)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueEnding(key, valueSuffix), this);
        }

        /// <summary>
        /// Find elements that have attributes whose value contains the match string. Case insensitive.
        /// </summary>
        /// <param name="key">name of the attribute</param>
        /// <param name="match">substring of value to search for</param>
        /// <returns>elements that have attributes containing this text</returns>
        public Elements GetElementsByAttributeValueContaining(string key, string match)
        {
            return Collector.Collect(new Evaluator.AttributeWithValueContaining(key, match), this);
        }

        /// <summary>
        /// Find elements whose sibling index is less than the supplied index.
        /// </summary>
        /// <param name="index">0-based index</param>
        /// <returns>elements less than index</returns>
        public Elements GetElementsByIndexLessThan(int index)
        {
            return Collector.Collect(new Evaluator.IndexLessThan(index), this);
        }

        /// <summary>
        /// Find elements whose sibling index is greater than the supplied index.
        /// </summary>
        /// <param name="index">0-based index</param>
        /// <returns>elements greater than index</returns>
        public Elements GetElementsByIndexGreaterThan(int index)
        {
            return Collector.Collect(new Evaluator.IndexGreaterThan(index), this);
        }

        /// <summary>
        /// Find elements whose sibling index is equal to the supplied index.
        /// </summary>
        /// <param name="index">0-based index</param>
        /// <returns>elements equal to index</returns>
        public Elements GetElementsByIndexEquals(int index)
        {
            return Collector.Collect(new Evaluator.IndexEquals(index), this);
        }

        /// <summary>
        /// Find elements that contain the specified string. The search is case insensitive. The text may appear directly 
        /// in the element, or in any of its descendants.
        /// </summary>
        /// <param name="searchText"></param>
        /// <returns>elements that contain the string, case insensitive.</returns>
        public Elements GetElementsContainingText(string searchText)
        {
            return Collector.Collect(new Evaluator.ContainsText(searchText), this);
        }

        /// <summary>
        /// Find all elements under this element (including self, and children of children).
        /// </summary>
        /// <returns>all elements</returns>
        public Elements GetAllElements()
        {
            return Collector.Collect(new Evaluator.AllElements(), this);
        }

        /// <summary>
        /// Gets the combined text of this element and all its children.
        /// </summary>
        public string Text
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                GetText(sb);
                return sb.ToString().Trim();
            }
            set
            {
                SetText(value);
            }
        }

        /// <summary> 
        /// </summary>
        /// <param name="accum"></param>
        private void GetText(StringBuilder accum)
        {
            foreach (Node child in ChildNodes)
            {
                if (child is TextNode)
                {
                    TextNode textNode = (TextNode)child;
                    string text = textNode.GetWholeText();

                    if (!PreserveWhitespace)
                    {
                        text = TextNode.NormaliseWhitespace(text);
                        if (TextNode.LastCharIsWhitespace(accum))
                            text = TextNode.StripLeadingWhitespace(text);
                    }
                    accum.Append(text);
                }
                else if (child is Element)
                {
                    Element element = (Element)child;
                    if (accum.Length > 0 && element.IsBlock && !TextNode.LastCharIsWhitespace(accum))
                    {
                        accum.Append(" ");
                    }
                    element.GetText(accum);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool PreserveWhitespace
        {
            get
            {
                return _tag.PreserveWhitespace || Parent != null && Parent.PreserveWhitespace;
            }
        }

        /// <summary>
        /// Set the text of this element. Any existing contents (text or elements) will be cleared
        /// </summary>
        /// <param name="text">unencoded text</param>
        /// <returns>this element</returns>
        public virtual Element SetText(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            Empty();
            TextNode textNode = new TextNode(text, BaseUri);
            AppendChild(textNode);

            return this;
        }

        /// <summary>
        /// Test if this element has any text content (that is not just whitespace).
        /// </summary>
        public bool HasText
        {
            get
            {
                foreach (Node child in ChildNodes)
                {
                    if (child is TextNode)
                    {
                        TextNode textNode = (TextNode)child;
                        if (!textNode.IsBlank)
                        {
                            return true;
                        }
                    }
                    else if (child is Element)
                    {
                        Element el = (Element)child;
                        if (el.HasText)
                            return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the combined data of this element. Data is e.g. the inside of a <code>script</code> tag.
        /// </summary>
        public string Data
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (Node childNode in ChildNodes)
                {
                    if (childNode is DataNode)
                    {
                        DataNode data = (DataNode)childNode;
                        sb.Append(data.GetWholeData());
                    }
                    else if (childNode is Element)
                    {
                        Element element = (Element)childNode;
                        string elementData = element.Data;
                        sb.Append(elementData);
                    }
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Gets the literal value of this element's "class" attribute, which may include multiple class names, space 
        /// separated. (E.g. on <code>&lt;div class="header gray"&gt;</code> returns, "<code>header gray</code>")
        /// </summary>
        public string ClassName
        {
            get { return Attributes.ContainsKey("class") ? Attributes.GetValue("class") : string.Empty; }
        }

        // TODO: Originally - Set<string>.


        /// <summary>
        /// Gets all of the element's class names. E.g. on element <code>&lt;div class="header gray"}&gt;</code>, 
        /// returns a set of two elements <code>"header", "gray"</code>. Note that modifications to this set are not pushed to 
        /// the backing <code>class</code> attribute; use the <seealso cref="ClassNames(HashSet)"/> method to persist them. 
        /// </summary>
        public HashSet<string> ClassNames
        {
            get
            {
                if (_classNames == null)
                {
                    string[] names = Regex.Split(ClassName, "\\s+");
                    _classNames = new HashSet<string>(names.ToList());
                }
                return _classNames;
            }
        }

        /// <summary>
        /// Set the element's <code>class</code> attribute to the supplied class names.
        /// </summary>
        /// <param name="classNames">set of classes</param>
        /// <returns>this element, for chaining</returns>
        public Element GetClassNames(HashSet<string> classNames)
        {
            if (classNames == null)
            {
                throw new ArgumentNullException("classNames");
            }

            Attributes.Add("class", string.Join(" ", classNames.ToArray()));
            return this;
        }

        /// <summary>
        /// Tests if this element has a class.
        /// </summary>
        /// <param name="className">name of class to check for</param>
        /// <returns>true if it does, false if not</returns>
        public bool HasClass(string className)
        {
            return ClassNames.Contains(className);
        }

        /// <summary>
        /// Add a class name to this element's <code>class</code> attribute.
        /// </summary>
        /// <param name="className">class name to add</param>
        /// <returns>this element</returns>
        public Element AddClass(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException("className");
            }

            HashSet<string> classes = ClassNames;
            classes.Add(className);
            GetClassNames(classes);

            return this;
        }

        /// <summary>
        /// Remove a class name from this element's <code>class</code> attribute.
        /// </summary>
        /// <param name="className">class name to remove</param>
        /// <returns>this element</returns>
        public Element RemoveClass(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException("className");
            }

            HashSet<string> classes = ClassNames;
            classes.Remove(className);
            GetClassNames(classes);

            return this;
        }

        /// <summary>
        /// Toggle a class name on this element's <code>class</code> attribute: if present, remove it; otherwise add it.
        /// </summary>
        /// <param name="className">class name to toggle</param>
        /// <returns>this element</returns>
        public Element ToggleClass(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException("className");
            }

            HashSet<string> classes = ClassNames;
            if (classes.Contains(className))
            {
                classes.Remove(className);
            }
            else
            {
                classes.Add(className);
            }
            GetClassNames(classes);

            return this;
        }

        /// <summary>
        /// Get the value of a form element (input, textarea, etc).
        /// </summary>
        /// <returns>the value of the form element, or empty string if not set.</returns>
        public String Val()
        {
            if (TagName.Equals("textarea", StringComparison.InvariantCultureIgnoreCase))
            {
                return Text;
            }
            else
            {
                return Attr("value");
            }
        }

        /// <summary>
        /// Set the value of a form element (input, textarea, etc).
        /// </summary>
        /// <param name="value">value to set</param>
        /// <returns>this element (for chaining)</returns>
        public Element Val(String value)
        {
            if (TagName.Equals("textarea", StringComparison.InvariantCultureIgnoreCase))
            {
                Text = value;
            }
            else
            {
                Attr("value", value);
            }
            return this;
        }

        public override void CreateOuterHtml(StringBuilder accum)
        {
            if (IsBlock || (Parent != null && Parent.Tag.CanContainBlock && SiblingIndex == 0))
                Indent(accum);
            accum
                    .Append("<")
                    .Append(TagName)
                    .Append(Attributes.Html);

            if (ChildNodes.Count <= 0 && _tag.IsEmpty)
            {
                accum.Append(" />");
            }
            else
            {
                accum.Append(">");
                Html(accum);
                if (_tag.CanContainBlock)
                {
                    Indent(accum);
                }
                accum.Append("</").Append(TagName).Append(">");
            }
        }

        /// <summary>
        /// Retrieves the element's inner HTML. E.g. on a <code>&lt;div&gt;</code> with one empty <code>&lt;p&gt;</code>, would return 
        /// <code>&lt;p&gt;&lt;/p&gt;</code>. (Whereas {@link #outerHtml()} would return <code>&lt;div&gt;&lt;p&gt;&lt;/p&gt;&lt;/div&gt;</code>.)
        /// </summary>
        /// <returns>string of HTML.</returns>
        /// <seealso cref="OuterHtml()"/>
        public string Html()
        {
            StringBuilder accum = new StringBuilder();
            Html(accum);
            return accum.ToString().Trim();
        }

        private void Html(StringBuilder accum)
        {
            foreach (Node node in ChildNodes)
                node.CreateOuterHtml(accum);
        }

        /// <summary>
        /// Set this element's inner HTML. Clears the existing HTML first.
        /// </summary>
        /// <param name="html">HTML to parse and set into this element</param>
        /// <returns>this element</returns>
        /// <seealso cref="Append(string)"/>
        public Element Html(string html)
        {
            Empty();
            Append(html);
            return this;
        }

        public override string ToString()
        {
            return OuterHtml();
        }

        public override bool Equals(Object o)
        {
            if (this == o) return true;
            if (!(o is Element)) return false;
            if (!base.Equals(o)) return false;

            Element element = (Element)o;

            if (Tag != null ? !Tag.Equals(element.Tag) : element.Tag != null) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = base.GetHashCode();
            result = 31 * result + (_tag != null ? _tag.GetHashCode() : 0);
            return result;
        }
    }
}
