using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Select
{

    /// <summary>
    /// A list of {@link Element Elements}, with methods that act on every element in the list
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Elements : IList<Element>
    {
        private List<Element> _contents;

        public Elements()
        {
            _contents = new List<Element>();
        }

        public Elements(ICollection<Element> elements)
        {
            _contents = new List<Element>(elements);
        }

        public Elements(List<Element> elements)
        {
            _contents = elements;
        }

        public Elements(params Element[] elements)
            : this(elements.ToList())
        {
        }

        // attribute methods
        /// <summary>
        /// Get an attribute value from the first matched element that has the attribute.
        /// </summary>
        /// <param name="attributeKey">The attribute key.</param>
        /// <returns>The attribute value from the first matched element that has the attribute.. If no elements were matched (isEmpty() == true), 
        /// or if the no elements have the attribute, returns empty string.</returns>
        /// <seealso cref="HasAttr(string)"/>
        public string Attr(string attributeKey)
        {
            foreach (Element element in _contents)
            {
                if (element.HasAttr(attributeKey))
                {
                    return element.Attr(attributeKey);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if any of the matched elements have this attribute set.
        /// </summary>
        /// <param name="attributeKey">attribute key</param>
        /// <returns>true if any of the elements have the attribute; false if none do.</returns>
        public bool HasAttr(string attributeKey)
        {
            foreach (Element element in _contents)
            {
                if (element.HasAttr(attributeKey))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set an attribute on all matched elements.
        /// </summary>
        /// <param name="attributeKey">attribute key</param>
        /// <param name="attributeValue">attribute value</param>
        /// <returns>this</returns>
        public Elements Attr(string attributeKey, string attributeValue)
        {
            foreach (Element element in _contents)
            {
                element.Attr(attributeKey, attributeValue);
            }
            return this;
        }

        /// <summary>
        /// Remove an attribute from every matched element.
        /// </summary>
        /// <param name="attributeKey">The attribute to remove.</param>
        /// <returns>this (for chaining)</returns>
        public Elements RemoveAttr(string attributeKey)
        {
            foreach (Element element in _contents)
            {
                element.RemoveAttr(attributeKey);
            }
            return this;
        }

        /// <summary>
        /// Add the class name to every matched element's <code>class</code> attribute.
        /// </summary>
        /// <param name="className">class name to add</param>
        /// <returns>this</returns>
        public Elements AddClass(string className)
        {
            foreach (Element element in _contents)
            {
                element.AddClass(className);
            }
            return this;
        }

        /// <summary>
        /// Remove the class name from every matched element's <code>class</code> attribute, if present.
        /// </summary>
        /// <param name="className">class name to remove</param>
        /// <returns>this</returns>
        public Elements RemoveClass(string className)
        {
            foreach (Element element in _contents)
            {
                element.RemoveClass(className);
            }
            return this;
        }

        /// <summary>
        /// Toggle the class name on every matched element's <code>class</code> attribute.
        /// </summary>
        /// <param name="className">class name to add if missing, or remove if present, from every element.</param>
        /// <returns>this</returns>
        public Elements ToggleClass(string className)
        {
            foreach (Element element in _contents)
            {
                element.ToggleClass(className);
            }
            return this;
        }

        /// <summary>
        /// Determine if any of the matched elements have this class name set in their <code>class</code> attribute.
        /// </summary>
        /// <param name="className">class name to check for</param>
        /// <returns>true if any do, false if none do</returns>
        public bool HasClass(string className)
        {
            foreach (Element element in _contents)
            {
                if (element.HasClass(className))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the form element's value of the first matched element.
        /// </summary>
        /// <returns>The form element's value, or empty if not set.</returns>
        /// <seealso cref="Element.Val()"/>
        public string Val()
        {
            if (Count > 0)
            {
                return First.Val();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Set the form element's value in each of the matched elements.
        /// </summary>
        /// <param name="value">The value to set into each matched element</param>
        /// <returns>this (for chaining)</returns>
        public Elements Val(string value)
        {
            foreach (Element element in _contents)
            {
                element.Val(value);
            }
            return this;
        }

        /// <summary>
        /// Gets the combined text of all the matched elements.
        /// </summary>
        /// <remarks>
        /// Note that it is possible to get repeats if the matched elements contain both parent elements and their own 
        /// children, as the Element.Text method returns the combined text of a parent and all its children.
        /// </remarks>
        /// <seealso cref="Element.Text"/>
        public string Text
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (Element element in _contents)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append(" ");
                    }
                    sb.Append(element.Text);
                }
                return sb.ToString();
            }
        }

        public bool HasText
        {
            get
            {
                foreach (Element element in _contents)
                {
                    if (element.HasText)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the combined inner HTML of all matched elements.
        /// </summary>
        /// <seealso cref="Text" />
        /// <seealso cref="OuterHtml"/>
        public string Html
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (Element element in _contents)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append("\n");
                    }
                    sb.Append(element.Html());
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Get the combined inner HTML of all matched elements.
        /// </summary>
        /// <seealso cref="Text"/>
        /// <seealso cref="Html"/>
        public string OuterHtml
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (Element element in _contents)
                {
                    if (sb.Length != 0)
                    {
                        sb.Append("\n");
                    }
                    sb.Append(element.OuterHtml());
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Set each matched element's inner HTML.
        /// </summary>
        /// <param name="html">HTML to parse and set into each matched element.</param>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Element.Html(string)"/>
        public Elements SetHtml(string html)
        {
            foreach (Element element in _contents)
            {
                element.Html(html);
            }
            return this;
        }

        /// <summary>
        /// Add the supplied HTML to the start of each matched element's inner HTML.
        /// </summary>
        /// <param name="html">HTML to add inside each element, before the existing HTML</param>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Element.Prepend(string)"/>
        public Elements Prepend(string html)
        {
            foreach (Element element in _contents)
            {
                element.Prepend(html);
            }
            return this;
        }

        /// <summary>
        /// Add the supplied HTML to the end of each matched element's inner HTML.
        /// </summary>
        /// <param name="html">HTML to add inside each element, after the existing HTML</param>
        /// <returns>this, for chaining</returns>
        /// <seealso cref="Element.Append(string)"/>
        public Elements Append(string html)
        {
            foreach (Element element in _contents)
            {
                element.Append(html);
            }
            return this;
        }

        /**
         Wrap the supplied HTML around each matched elements. For example, with HTML
         {@code <p><b>This</b> is <b>Jsoup</b></p>},
         <code>doc.select("b").wrap("&lt;i&gt;&lt;/i&gt;");</code>
         becomes {@code <p><i><b>This</b></i> is <i><b>jsoup</b></i></p>}
         @param html HTML to wrap around each element, e.g. {@code <div class="head"></div>}. Can be arbitralily deep.
         @return this (for chaining)
         @see Element#wrap
         */
        public Elements Wrap(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                throw new ArgumentNullException("html");
            }
            foreach (Element element in _contents)
            {
                element.Wrap(html);
            }
            return this;
        }

        // filters

        /// <summary>
        /// Find matching elements within this element list.
        /// </summary>
        /// <param name="query">A selector query</param>
        /// <returns>the filtered list of elements, or an empty list if none match.</returns>
        public Elements Select(string query)
        {
            return Selector.Select(query, this);
        }

        /// <summary>
        /// Reduce the matched elements to one element
        /// </summary>
        /// <param name="index">the (zero-based) index of the element in the list to retain</param>
        /// <returns>Elements containing only the specified element, or, if that element did not exist, an empty list.</returns>
        public Elements Eq(int index)
        {
            if (_contents.Count > index)
            {
                return new Elements(this[index]);
            }
            else
            {
                return new Elements();
            }
        }

        /// <summary>
        /// Test if any of the matched elements match the supplied query.
        /// </summary>
        /// <param name="query">A selector</param>
        /// <returns>true if at least one element in the list matches the query.</returns>
        public bool Is(string query)
        {
            Elements children = this.Select(query);
            return !children.IsEmpty;
        }

        // list-like methods
        /// <summary>
        /// Gets the first matched element or <code>null</code> if contents is empty.
        /// </summary>
        public Element First
        {
            get { return _contents.Count > 0 ? _contents[0] : null; }
        }

        /// <summary>
        /// Gets the last matched element or <code>null</code> if contents is empty.
        /// </summary>
        public Element Last
        {
            get { return _contents.Count > 0 ? _contents[_contents.Count - 1] : null; }
        }

        // implements List<Element> delegates:
        public int Count { get { return _contents.Count; } }

        public bool IsEmpty { get { return _contents.Count <= 0; } }

        public bool Contains(Element o) { return _contents.Contains(o); }

        public Element[] ToArray() { return _contents.ToArray(); }

        //public T[] ToArray<T>(T[] a) {return _contents.ToArray<T>(); } // TODO: What the hell does this function do?

        public bool Add(Element element)
        {
            _contents.Add(element);
            return true;
        }

        public bool ContainsAll(IEnumerable<Element> e) { return e.All(el => _contents.Contains(el)); }

        public bool AddRange(IEnumerable<Element> e) { _contents.AddRange(e); return true; }

        public bool InsertRange(int index, IEnumerable<Element> e)
        {
            _contents.InsertRange(index, e);
            return true;
        }

        public bool RemoveAll(IEnumerable<Element> e)
        {
            foreach (Element item in e)
            {
                _contents.Remove(item);
            }
            return true;
        }

        public bool RetainAll(IEnumerable<Element> e)
        {
            _contents = _contents.Intersect(e).ToList();
            return true;
        }

        public void Clear() { _contents.Clear(); }

        public override bool Equals(Object o) { return _contents.Equals(o); }

        public override int GetHashCode() { return _contents.GetHashCode(); }

        public Element this[int index]
        {
            get { return _contents[index]; }
            set
            {
                _contents[index] = value;
            }
        }

        public void Insert(int index, Element element) { _contents.Insert(index, element); }

        /*public Element RemoveAt(int index) {
            _contents.RemoveAt(index); return true; 
        }*/

        public int IndexOf(Element e) { return _contents.IndexOf(e); }

        public int LastIndexOf(Element e) { return _contents.LastIndexOf(e); }

        //public ListIterator<Element> listIterator() {return contents.listIterator();}

        //public ListIterator<Element> listIterator(int index) {return contents.listIterator(index);}

        public List<Element> SubList(int fromIndex, int toIndex) { return _contents.GetRange(fromIndex, toIndex - fromIndex); }

        #region IList<Element> Members


        void IList<Element>.RemoveAt(int index)
        {
            _contents.RemoveAt(index);
        }

        #endregion

        #region ICollection<Element> Members

        void ICollection<Element>.Add(Element item)
        {
            _contents.Add(item);
        }

        public void CopyTo(Element[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Element item)
        {
            return _contents.Remove(item);
        }

        #endregion

        #region IEnumerable<Element> Members

        IEnumerator<Element> IEnumerable<Element>.GetEnumerator()
        {
            return _contents.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _contents.GetEnumerator();
        }

        #endregion
    }
}
