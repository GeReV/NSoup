using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Helper
{
    /**
 * A minimal String utility class. Designed for internal jsoup use only.
 */
    public static class StringUtil
    {
        // memoised padding up to 10
        private static readonly string[] padding = { "", " ", "  ", "   ", "    ", "     ", "      ", "       ", "        ", "         ", "          " };

        /// <summary>
        /// Join a collection of strings by a seperator
        /// </summary>
        /// <param name="strings">collection of string objects</param>
        /// <param name="sep">string to place between strings</param>
        /// <returns>joined string</returns>
        public static string Join(this ICollection<string> strings, string sep)
        {
            return string.Join(sep, strings.ToArray());
        }

        /**
         * Join a collection of strings by a seperator
         * @param strings iterator of string objects
         * @param sep string to place between strings
         * @return joined string
         */
        /*public static String join(Iterator<String> strings, String sep) {
            if (!strings.hasNext())
                return "";

            String start = strings.next();
            if (!strings.hasNext()) // only one, avoid builder
                return start;

            StringBuilder sb = new StringBuilder(64).append(start);
            while (strings.hasNext()) {
                sb.append(sep);
                sb.append(strings.next());
            }
            return sb.toString();
        }*/

        /// <summary>
        /// Returns space padding
        /// </summary>
        /// <param name="width">amount of padding desired</param>
        /// <returns>string of spaces * width</returns>
        public static string Padding(int width)
        {
            if (width < 0)
            {
                throw new ArgumentException("width must be > 0");
            }

            if (width < padding.Length)
            {
                return padding[width];
            }

            return string.Empty.PadLeft(width);
        }

        /**
         * Tests if a string is blank: null, emtpy, or only whitespace (" ", \r\n, \t, etc)
         * @param string string to test
         * @return if string is blank
         */
        public static bool IsBlank(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return true;
            }

            return s.Trim().Length == 0;
        }

        /**
         * Tests if a string is numeric, i.e. contains only digit characters
         * @param string string to test
         * @return true if only digit chars, false if empty or null or contains non-digit chrs
         */
        public static bool IsNumeric(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            bool anyNonDigits = s.ToCharArray().Any(c => !char.IsDigit(c)); // Check if there are any non-digits. Used this so algorithm won't have to run on all chars.

            return !(anyNonDigits);
        }

        /// <summary>
        /// Tests if a code point is "whitespace" as defined in the HTML spec.
        /// </summary>
        /// <param name="c">Code point to test</param>
        /// <returns>True if code point is whitespace, false otherwise</returns>
        public static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n' || c == '\f' || c == '\r';
        }

        public static string NormaliseWhitespace(this string s)
        {
            StringBuilder sb = new StringBuilder(s.Length);

            bool lastWasWhite = false;
            bool modified = false;

            int l = s.Length;
            for (int i = 0; i < l; i++)
            {
                char c = s[i];
                if (IsWhiteSpace(c))
                {
                    if (lastWasWhite)
                    {
                        modified = true;
                        continue;
                    }
                    if (c != ' ')
                    {
                        modified = true;
                    }
                    sb.Append(' ');
                    lastWasWhite = true;
                }
                else
                {
                    sb.Append(c);
                    lastWasWhite = false;
                }
            }
            return modified ? sb.ToString() : s;
        }

        public static bool In(string needle, params string[] haystack)
        {
            foreach (string hay in haystack)
            {
                if (hay.Equals(needle))
                {
                    return true;
                }
            }
            return false;
        }
    }

}
