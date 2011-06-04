using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace NSoup.Nodes
{
    /// <summary>
    /// The attributes of an Element.
    /// </summary>
    /// <remarks>
    /// Attributes are treated as a map: there can be only one value associated with an attribute key.
    /// Attribute key and value comparisons are done case insensitively, and keys are normalised to
    /// lower-case.
    /// </remarks>
    /// <!--
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class Attributes : IEnumerable<Attribute>, IEquatable<Attributes>, ICloneable
    {
        public static readonly string DataPrefix = "data-";

        protected Dictionary<string, Attribute> attributes = new Dictionary<string, Attribute>(2);
        // The order in which items are returned from a Dictionary is undefined. Should find a different solution.
        //private LinkedHashMap<string, Attribute> attributes = new LinkedHashMap<string, Attribute>();
        // linked hash map to preserve insertion order.

        /// <summary>
        /// Set a new attribute, or replace an existing one by key.
        /// </summary>
        /// <param name="key">attribute key</param>
        /// <param name="value">attribute value</param>
        public void Add(string key, string value)
        {
            Attribute attr = new Attribute(key, value);
            Add(attr);
        }

        /// <summary>
        /// Set a new attribute, or replace an existing one by key.
        /// </summary>
        /// <param name="attribute">attribute</param>
        public void Add(Attribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException("attribute");
            }

            if (attributes.ContainsKey(attribute.Key))
            {
                attributes[attribute.Key] = attribute;
                return;
            }
            attributes.Add(attribute.Key, attribute);
        }

        /// <summary>
        /// Get an attribute value by key.
        /// </summary>
        /// <param name="key">the attribute key</param>
        /// <returns>the attribute value if set; or empty string if not set.</returns>
        /// <see cref="HasKey(string)"/>
        public string GetValue(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            Attribute attr = null;
            attributes.TryGetValue(key.ToLowerInvariant(), out attr);
            return (attr != null) ? attr.Value : string.Empty;
        }

        public string this[string key]
        {
            get { return GetValue(key); }
            set { Add(key, value); }
        }

        /// <summary>
        /// Remove an attribute by key.
        /// </summary>
        /// <param name="key">attribute key to remove</param>
        public void Remove(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            attributes.Remove(key.ToLowerInvariant());
        }

        /// <summary>
        /// Tests if these attributes contain an attribute with this key.
        /// </summary>
        /// <param name="key">key to check for</param>
        /// <returns>true if key exists, false otherwise</returns>
        public bool ContainsKey(string key)
        {
            return attributes.ContainsKey(key.ToLowerInvariant());
        }

        /// <summary>
        /// Gets the number of attributes in this set.
        /// </summary>
        public int Count
        {
            get { return attributes.Count; }
        }

        /// <summary>
        /// Add all the attributes from the incoming set to this set.
        /// </summary>
        /// <param name="incoming">attributes to add to these attributes.</param>
        public void AddRange(Attributes incoming)
        {
            foreach (Attribute item in incoming.attributes.Values)
            {
                Add(item);
            }
        }

        /// <summary>
        /// Gets the attributes as a List, for iteration.
        /// </summary>
        /// <remarks>
        /// Do not modify the keys of the attributes via this view, as changes 
        /// to keys will not be recognised in the containing set.
        /// </remarks>
        public ReadOnlyCollection<Attribute> AsList
        {
            get
            {
                List<Attribute> list = new List<Attribute>(attributes.Count);
                foreach (KeyValuePair<string, Attribute> entry in attributes)
                {
                    list.Add(entry.Value);
                }
                return list.AsReadOnly(); // TODO: Solve this - System.Collections.rea Collections.unmodifiableList(list);
            }
        }

        /// <summary>
        /// Retrieves a filtered view of attributes that are HTML5 custom data attributes; that is, attributes with keys
        /// starting with <code>data-</code>.
        /// </summary>
        /// <returns>map of custom data attributes.</returns>
        public IDictionary<string, string> GetDataset()
        {
            return new Dataset(attributes);
        }

        /// <summary>
        /// Gets the HTML representation of these attributes.
        /// </summary>
        public string Html()
        {
            StringBuilder accum = new StringBuilder();
            Html(accum, (new Document(string.Empty).Settings)); // output settings a bit funky, but this html() seldom used
            return accum.ToString();
        }

        public void Html(StringBuilder accum, Document.OutputSettings output)
        {
            foreach (Attribute attribute in attributes.Values)
            {
                accum.Append(" ");
                attribute.Html(accum, output);
            }
        }

        public override string ToString()
        {
            return Html();
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!(obj is Attributes)) return false;

            Attributes that = (Attributes)obj;

            if (attributes != null ? !attributes.Equals(that.attributes) : that.attributes != null) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return attributes != null ? attributes.GetHashCode() : 0;
        }

        /// <summary>
        /// A dictionary which filters through the list of attribute and works only against data attributes (attributes prefixed with "data-").
        /// </summary>
        private class Dataset : IDictionary<string, string>
        {
            private Dictionary<string, Attribute> _attributes;

            public Dataset(Dictionary<string, Attribute> attributes)
            {
                _attributes = attributes;
            }

            #region IDictionary<string,string> Members

            void IDictionary<string, string>.Add(string key, string value)
            {
                string dataKey = DataKey(key);
                _attributes.Add(dataKey, new Attribute(dataKey, value));
            }

            bool IDictionary<string, string>.ContainsKey(string key)
            {
                return _attributes.ContainsKey(DataKey(key));
            }

            ICollection<string> IDictionary<string, string>.Keys
            {
                get
                {
                    return _attributes
                        .Where(pair => pair.Value.IsDataAttribute) // Get all data attributes;
                        .Select(k => k.Key.Substring(DataPrefix.Length)) // Filter their prefix;
                        .ToArray(); // Return as array.
                }
            }

            bool IDictionary<string, string>.Remove(string key)
            {
                return _attributes.Remove(DataKey(key));
            }

            bool IDictionary<string, string>.TryGetValue(string key, out string value)
            {
                Attribute attr = null;
                bool success = _attributes.TryGetValue(DataKey(key), out attr);

                value = attr.Value;

                return success;
            }

            ICollection<string> IDictionary<string, string>.Values
            {
                get
                {
                    return _attributes
                        .Where(pair => pair.Value.IsDataAttribute) // Get all data attributes;
                        .Select(pair => pair.Value.Value) // Get their values;
                        .ToArray(); // Return as array.
                }
            }

            string IDictionary<string, string>.this[string key]
            {
                set
                {
                    string dataKey = Attributes.DataKey(key);
                    string oldValue = _attributes.ContainsKey(dataKey) ? _attributes[dataKey].Value : null;
                    Attribute attr = new Attribute(dataKey, value);
                    _attributes[dataKey] = attr;
                }
                get
                {
                    string dataKey = Attributes.DataKey(key);

                    Attribute attr = _attributes[dataKey];
                    if (attr != null && attr.IsDataAttribute) // If attribute
                    {
                        return attr.Value;
                    }
                    return null;
                }
            }

            #endregion

            #region ICollection<KeyValuePair<string,string>> Members

            void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
            {
                KeyValuePair<string, Attribute> attr = GetDataAttributeKeyValuePair(item.Key, item.Value);
                _attributes.Add(attr.Key, attr.Value);
            }

            void ICollection<KeyValuePair<string, string>>.Clear()
            {
                foreach (KeyValuePair<string, Attribute> item in _attributes.Where(pair => pair.Value.IsDataAttribute))
                {
                    _attributes.Remove(item.Key);
                }
            }

            bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
            {
                return _attributes.Contains(GetDataAttributeKeyValuePair(item.Key, item.Value));
            }

            void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            int ICollection<KeyValuePair<string, string>>.Count
            {
                get { return _attributes.Where(pair => pair.Value.IsDataAttribute).Count(); }
            }

            bool ICollection<KeyValuePair<string, string>>.IsReadOnly
            {
                get { return false; }
            }

            bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
            {
                KeyValuePair<string, Attribute> attr = GetDataAttributeKeyValuePair(item.Key, item.Value);
                return _attributes.Remove(attr.Key);
            }

            #endregion

            #region IEnumerable<KeyValuePair<string,string>> Members

            IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
            {
                return new DatasetEnumerator(_attributes);
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new DatasetEnumerator(_attributes);
            }

            #endregion

            /// <summary>
            /// Creates a KeyValuePair with a data attribute to be inserted into the attribute collection.
            /// </summary>
            /// <param name="key">Data attribute key, without the prefix.</param>
            /// <param name="value">Data attribute's value.</param>
            /// <returns>Data attribute (KeyValuePair<string, Attribute>) based on passed values.</returns>
            private KeyValuePair<string, Attribute> GetDataAttributeKeyValuePair(string key, string value)
            {
                string dataKey = DataKey(key);
                return new KeyValuePair<string, Attribute>(dataKey, new Attribute(dataKey, value));
            }

            private class DatasetEnumerator : IEnumerator<KeyValuePair<string, string>>
            {
                private Dictionary<string, Attribute> _attributes;
                private Attribute _attr;
                private Dictionary<string, Attribute>.ValueCollection.Enumerator _attrIter;

                public DatasetEnumerator(Dictionary<string, Attribute> attributes)
                {
                    _attributes = attributes;
                    _attrIter = _attributes.Values.GetEnumerator();
                }

                public void Remove()
                {
                    _attributes.Remove(_attr.Key); // TODO: This might become a problem. Can't remove when using an iterator.
                }

                #region IEnumerator<KeyValuePair<string,string>> Members

                public KeyValuePair<string, string> Current
                {
                    get { return new KeyValuePair<string, string>(_attr.Key.Substring(DataPrefix.Length), _attr.Value); }
                }

                #endregion

                #region IDisposable Members

                public void Dispose()
                {
                    return;
                }

                #endregion

                #region IEnumerator Members

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    while (_attrIter.MoveNext())
                    {
                        _attr = _attrIter.Current;
                        if (_attr.IsDataAttribute)
                        {
                            return true;
                        }
                    }
                    return false;
                }

                public void Reset()
                {
                    _attrIter = _attributes.Values.GetEnumerator();
                }

                #endregion
            }
        }

        private static string DataKey(string key)
        {
            return DataPrefix + key;
        }

        #region IEnumerable<Attribute> Members

        IEnumerator<Attribute> IEnumerable<Attribute>.GetEnumerator()
        {
            return AsList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return AsList.GetEnumerator();
        }

        #endregion

        #region IEquatable<Attributes> Members

        public bool Equals(Attributes other)
        {
            return Equals(other as object);
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            Attributes clone = new Attributes();

            clone.attributes = new Dictionary<string, Attribute>(attributes.Count);

            foreach (Attribute attribute in this)
            {
                clone.attributes[attribute.Key] = (Attribute)attribute.Clone();
            }

            return clone;
        }

        #endregion
    }
}