
using NSoup.Parse;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NSoup.Nodes
{
    /// <summary>
    /// HTML entities, and escape routines.
    /// Source: <a href="http://www.w3.org/TR/html5/named-character-references.html#named-character-references">W3C HTML named character references</a>.
    /// </summary>
    public class Entities
    {
        public class EscapeMode
        {
            public static readonly EscapeMode Xhtml = new EscapeMode(_xhtmlByVal);
            public static readonly EscapeMode Base = new EscapeMode(_baseByVal);
            public static readonly EscapeMode Extended = new EscapeMode(_fullByVal);

            private Dictionary<char, string> _map;

            private EscapeMode(Dictionary<char, string> map)
            {
                this._map = map;
            }

            public Dictionary<char, string> GetMap()
            {
                return this._map;
            }
        }

        private static readonly Dictionary<string, char> _full;
        private static readonly Dictionary<char, string> _xhtmlByVal;
        private static readonly Dictionary<string, char> _base;
        private static readonly Dictionary<char, string> _baseByVal;
        private static readonly Dictionary<char, string> _fullByVal;
        private static readonly Regex _unescapePattern = new Regex("&(#(x|X)?([0-9a-fA-F]+)|[a-zA-Z]+\\d*);?", RegexOptions.Compiled);
        private static readonly Regex _strictUnescapePattern = new Regex("&(#(x|X)?([0-9a-fA-F]+)|[a-zA-Z]+\\d*);", RegexOptions.Compiled);

        private Entities()
        {
        }

        /// <summary>
        /// Check if the input is a known named entity
        /// </summary>
        /// <param name="name">the possible entity name (e.g. "lt" or "amp")</param>
        /// <returns>true if a known named entity</returns>
        public static bool IsNamedEntity(string name)
        {
            return _full.ContainsKey(name);
        }

        /// <summary>
        /// Check if the input is a known named entity in the base entity set.
        /// </summary>
        /// <param name="name">The possible entity name (e.g. "lt" or "amp")</param>
        /// <returns>True if a known named entity in the base set</returns>
        /// <see cref="IsNamedEntity(string)"/>
        public static bool IsBaseNamedEntity(string name)
        {
            return _base.ContainsKey(name);
        }

        /// <summary>
        /// Get the Character value of the named entity
        /// </summary>
        /// <param name="name">named entity (e.g. "lt" or "amp")</param>
        /// <returns>the Character value of the named entity (e.g. '<' or '&')</returns>
        public static char GetCharacterByName(string name)
        {
            return _full[name];
        }

        public static string Escape(string s, OutputSettings output)
        {
            return Escape(s, output.Encoding, output.EscapeMode);
        }

        public static string Escape(string s, Encoding encoding, EscapeMode escapeMode)
        {
            StringBuilder accum = new StringBuilder(s.Length * 2);
            Dictionary<char, string> map = escapeMode.GetMap();

            for (int pos = 0; pos < s.Length; pos++)
            {
                char c = s[pos];

                // To replicate this line from the original code: encoder.canEncode(c), we perform the following steps:
                byte[] unicodeBytes = Encoding.Unicode.GetBytes(c.ToString()); // Get the bytes by unicode.

                byte[] encodingBytes = Encoding.Convert(Encoding.Unicode, encoding, unicodeBytes); // Convert bytes into target encoding's bytes.

                char[] encodingChars = new char[encoding.GetCharCount(encodingBytes)];
                encoding.GetChars(encodingBytes, 0, encodingBytes.Length, encodingChars, 0); // Convert target encoding bytes into chars.

                if (map.ContainsKey(c))
                {
                    accum.AppendFormat("&{0};", map[c]);
                }
                else if (encodingChars[0] == c)
                {
                    // If the char resulting from the conversion matches the original one, we can understand that the encoding can encode this char.
                    accum.Append(c); // If so, add it to the accumulator.
                }
                else
                {
                    accum.AppendFormat("&#{0};", (int)c);
                }
            }

            return accum.ToString();
        }

        public static string Unescape(string s)
        {
            return Unescape(s, false);
        }

        /// <summary>
        /// Unescape the input string.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="strict">if "strict" (that is, requires trailing ';' char, otherwise that's optional)</param>
        /// <returns></returns>
        public static string Unescape(string s, bool strict)
        {
            return Parser.UnescapeEntities(s, strict);
        }

        // xhtml has restricted entities
        private static readonly object[,] xhtmlArray = {
            {"quot", 0x00022},
            {"amp", 0x00026},
            {"apos", 0x00027},
            {"lt", 0x0003C},
            {"gt", 0x0003E}
        };

        static Entities()
        {
            _xhtmlByVal = new Dictionary<char, string>(xhtmlArray.Length);
            _base = LoadEntities("NSoup.Nodes.entities-base.txt");
            _baseByVal = ToCharacterKey(_base);
            _full = LoadEntities("NSoup.Nodes.entities-full.txt");
            _fullByVal = ToCharacterKey(_full);

            for (int i = 0; i < xhtmlArray.Length / 2; i++)
            {
                char c = (char)(int)xhtmlArray[i, 1];
                _xhtmlByVal[c] = ((string)xhtmlArray[i, 0]);
            }
        }

        private static Dictionary<string, char> LoadEntities(string resourceName)
        {
            Dictionary<string, char> entities = new Dictionary<string, char>();

            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string line = reader.ReadLine();
                string[] parts;


                while (!string.IsNullOrEmpty(line))
                {
                    line = reader.ReadLine();

                    if (!string.IsNullOrEmpty(line))
                    {
                        parts = line.Split('=');

                        entities.Add(parts[0], (char)Convert.ToInt32(parts[1], 16));
                    }
                }
            }

            return entities;
        }

        private static Dictionary<char, string> ToCharacterKey(Dictionary<string, char> inMap)
        {
            Dictionary<char, string> outMap = new Dictionary<char, string>();
            foreach (KeyValuePair<string, char> entry in inMap)
            {
                char character = entry.Value;
                string name = entry.Key;

                if (outMap.ContainsKey(character))
                {
                    // dupe, prefer the lower case version
                    if (name.ToLowerInvariant().Equals(name))
                    {
                        outMap[character] = name;
                    }
                }
                else
                {
                    outMap[character] = name;
                }
            }
            return outMap;
        }
    }
}