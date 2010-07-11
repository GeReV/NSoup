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
    public class Attributes : IEnumerable<Attribute>, IEquatable<Attributes>
    {
        private Dictionary<string, Attribute> attributes = new Dictionary<string, Attribute>(2);
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
        /// Gets the HTML representation of these attributes.
        /// </summary>
        public string Html()
        {
                StringBuilder accum = new StringBuilder();
                Html(accum);
                return accum.ToString();
        }

        private void Html(StringBuilder accum)
        {
            foreach (Attribute attribute in this)
            {
                accum.Append(" ");
                attribute.Html(accum);
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
    }
}