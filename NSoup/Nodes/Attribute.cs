using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace NSoup.Nodes
{
    
    /// <summary>
    /// A single key + value attribute. Keys are trimmed and normalised to lower-case.
    /// </summary>
    /// <!-- 
    /// Original Author: Jonathan Hedley, jonathan@hedley.net
    /// Ported to .NET by: Amir Grozki
    /// -->   
    public class Attribute : IEquatable<Attribute>
    {
        private string _key;
        private string _value;

        /// <summary>
        /// Create a new attribute from unencoded (raw) key and value. 
        /// </summary>
        /// <param name="key">attribute key</param>
        /// <param name="value">attribute value</param>
        /// <see cref="createFromEncoded"/>
        public Attribute(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            if (value == null) // Value may be empty ('alt', for example).
            {
                throw new ArgumentNullException("value");
            }

            this._key = key.Trim().ToLowerInvariant();
            this._value = value;
        }

        /// <summary>
        /// Gets or sets the Key property.
        /// </summary>
        public string Key
        {
            get { return _key; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }
                this._key = value.Trim().ToLowerInvariant();
            }
        }

        /// <summary>
        /// Gets or sets the Value property.
        /// </summary>
        public string Value
        {
            get { return _value; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                this._value = value;
            }
        }

        /// <summary>
        /// Get the attribute value.
        /// </summary>
        /// <returns>the attribute value</returns>
        public string GetValue()
        {
            return Value;
        }

        /// <summary>
        /// Set the attribute value.
        /// </summary>
        /// <param name="value">the new attribute value; must not be null</param>
        /// <returns>old value</returns>
        public string SetValue(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            string old = this.Value;

            this.Value = value;

            return old;
        }

        /// <summary>
        /// Get the HTML representation of this attribute; e.g. <code>href="index.html"</code>
        /// </summary>
        public string Html()
        {
            return string.Format("{0}=\"{1}\"", _key, Entities.Escape(_value, (new Document(string.Empty).Settings)));
        }

        public void Html(StringBuilder accum, Document.OutputSettings output)
        {
            accum.Append(Key)
                .Append("=\"")
                .Append(Entities.Escape(Value, output))
                .Append("\"");
        }

        /// <summary>
        /// Get the string representation of this attribute, implemented as <see cref="Html()"/>.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.Html();
        }

        /// <summary>
        /// Create a new Attribute from an unencoded key and a HMTL attribute encoded value.
        /// </summary>
        /// <param name="unencodedKey">assumes the key is not encoded, as can be only run of simple \w chars.</param>
        /// <param name="encodedValue">HTML attribute encoded value</param>
        /// <returns>attribute</returns>
        public static Attribute CreateFromEncoded(string unencodedKey, string encodedValue)
        {
            string value = Entities.Unescape(encodedValue);
            return new Attribute(unencodedKey, value);
        }

        public bool IsDataAttribute
        {
            get { return Key.StartsWith(Attributes.DataPrefix) && Key.Length > Attributes.DataPrefix.Length; }
        }

        public override int GetHashCode()
        {
            int result = _key != null ? _key.GetHashCode() : 0;
            result = 31 * result + (_value != null ? _value.GetHashCode() : 0);
            return result;
        }

        #region IEquatable<Attribute> Members

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (!(obj is Attribute)) return false;

            Attribute attribute = (Attribute)obj;

            if (this._key != null ? !this._key.Equals(attribute.Key) : attribute.Key != null) return false;
            if (this._value != null ? !this._value.Equals(attribute.Value) : attribute.Value != null) return false;

            return true;
        }

        public bool Equals(Attribute other)
        {
            return Equals(other as object);
        }

        #endregion
    }
}
