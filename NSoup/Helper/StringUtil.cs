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

        public static string Join(IEnumerator<string> iterator, string sep)
        {
            if (!iterator.MoveNext())
            {
                return string.Empty;
            }

            var start = iterator.Current;
            if (!iterator.MoveNext())
            {
                return start;
            }

            var sb = new StringBuilder(64).Append(start);
            while (iterator.MoveNext())
            {
                sb.Append(sep);
                sb.Append(iterator.Current);
            }

            return sb.ToString();
        }

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

        public static bool IsBlank(this string s)
        {
            return string.IsNullOrWhiteSpace(s) ? true : s.Trim().Length == 0;
        }

        public static bool IsNumeric(this string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            var anyNonDigits = s.ToCharArray().Any(c => !char.IsDigit(c));
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
            var sb = new StringBuilder(s.Length);

            var lastWasWhite = false;
            var reachedNonWhite = false;

            var l = s.Length;
            for (var i = 0; i < l; i++)
            {
                var c = s[i];
                if (IsWhiteSpace(c))
                {
                    if (lastWasWhite) { continue; }
                    sb.Append(' ');
                    lastWasWhite = true;
                }
                else
                {
                    sb.Append(c);
                    lastWasWhite = false;
                    reachedNonWhite = true;
                }
            }

            return sb.ToString();
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

        public static bool InSorted(string needle, params string[] haystack)
        {
            return Array.BinarySearch(haystack, needle) >= 0;
        }

        public static Uri Resolve(Uri url, string relUrl)
        {
            Uri resultUri = null;
            if (relUrl.IndexOf('.') == 0 && url.PathAndQuery.IndexOf('/') != 0)
            {
                url = new Uri(url.Scheme + url.Host + url.Port + "/" + url.PathAndQuery);
            }

            Uri.TryCreate(url, relUrl, out resultUri);
            return resultUri;
        }

        public static string Resolve(string url, string relUrl)
        {
            Uri baseUri = null;
            var validUri = Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out baseUri);
            if (validUri)
            {
                var resultUri = Resolve(baseUri, relUrl);
                return resultUri == null ? string.Empty : resultUri.ToString();
            }

            validUri = Uri.TryCreate(relUrl, UriKind.RelativeOrAbsolute, out baseUri);
            return baseUri == null ? string.Empty : baseUri.ToString();
        }
    }
}