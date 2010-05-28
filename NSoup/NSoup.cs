using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;
using NSoup.Parse;
using System.IO;
using NSoup.Safety;

namespace NSoup
{
    /// <summary>
    /// The core public access point to the NSoup functionality.
    /// </summary>
    /// <!--
    /// Changed to NSoupClient due to namespace collisions.
    /// 
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class NSoupClient
    {
        private NSoupClient() { }

        /// <summary>
        /// Parse HTML into a Document. The parser will make a sensible, balanced document tree out of any HTML.
        /// </summary>
        /// <param name="html">HTML to parse</param>
        /// <param name="baseUri">The URL where the HTML was retrieved from. Used to resolve relative URLs to absolute URLs, that occur 
        /// before the HTML declares a <code>&lt;base href&gt;</code> tag.</param>
        /// <returns>sane HTML</returns>
        public static Document Parse(string html, string baseUri)
        {
            return Parser.Parse(html, baseUri);
        }

        /// <summary>
        /// Parse HTML into a Document. As no base URI is specified, absolute URL detection relies on the HTML including a 
        /// <code>&lt;base href&gt;</code> tag.
        /// </summary>
        /// <param name="html">HTML to parse</param>
        /// <returns>sane HTML</returns>
        /// <seealso cref="Parse(string, string)"/>
        public static Document Parse(string html)
        {
            return Parser.Parse(html, string.Empty);
        }

        /// <summary>
        /// Fetch a URL, and parse it as HTML.
        /// </summary>
        /// <param name="url">URL to fetch (with a GET). The protocol must be <code>http</code> or <code>https</code>.</param>
        /// <param name="timeoutMillis">Connection and read timeout, in milliseconds. If exceeded, IOException is thrown.</param>
        /// <returns>The parsed HTML.</returns>
        /// <remarks>Throws an exception if the final server response != 200 OK (redirects aren't followed), or if there's an error reading the response stream.</remarks>
        public static Document Parse(Uri url, int timeoutMillis)
        {
            string html = DataUtil.Load(url, timeoutMillis);
            return Parse(html, url.ToString());
        }

        /// <summary>
        /// Parse the contents of a file as HTML.
        /// </summary>
        /// <param name="filename">file to load HTML from</param>
        /// <param name="charsetName">character set of file contents. If you don't know the charset, generally the best guess is <code>UTF-8</code>.</param>
        /// <param name="baseUri">The URL where the HTML was retrieved from, to generate absolute URLs relative to.</param>
        /// <returns>sane HTML</returns>
        /// <remarks>Throws an exception if the file could not be found, or read, or if the charsetName is invalid.</remarks>
        public static Document Parse(string filename, string charsetName, string baseUri)
        {
            string html = DataUtil.Load(filename, charsetName);
            return Parse(html, baseUri);
        }

        /// <summary>
        /// Parse the contents of a file as HTML. The location of the file is used as the base URI to qualify relative URLs.    
        /// </summary>
        /// <param name="filename">file to load HTML from</param>
        /// <param name="charsetName">character set of file contents. If you don't know the charset, generally the best guess is <code>UTF-8</code>.</param>
        /// <returns>sane HTML</returns>
        /// <remarks>if the file could not be found, or read, or if the charsetName is invalid.</remarks>
        /// <seealso cref="parse(string, string, string)"/>
        public static Document parse(string filename, string charsetName)
        {
            string html = DataUtil.Load(filename, charsetName);
            return Parse(html, Path.GetDirectoryName(filename));
        }

        /// <summary>
        /// Parse a fragment of HTML, with the assumption that it forms the {@code body} of the HTML.
        /// </summary>
        /// <param name="bodyHtml">body HTML fragment</param>
        /// <param name="baseUri">URL to resolve relative URLs against.</param>
        /// <returns>sane HTML document</returns>
        /// <seealso cref="Document.Body()"/>
        public static Document ParseBodyFragment(string bodyHtml, string baseUri)
        {
            return Parser.ParseBodyFragment(bodyHtml, baseUri);
        }

        /// <summary>
        /// Parse a fragment of HTML, with the assumption that it forms the <code>body</code> of the HTML.
        /// </summary>
        /// <param name="bodyHtml">body HTML fragment</param>
        /// <returns>sane HTML document</returns>
        /// <seealso cref="Document.Body()"/>
        public static Document ParseBodyFragment(string bodyHtml)
        {
            return Parser.ParseBodyFragment(bodyHtml, string.Empty);
        }

        /// <summary>
        /// Get safe HTML from untrusted input HTML, by parsing input HTML and filtering it through a white-list of permitted 
        /// tags and attributes.
        /// </summary>
        /// <param name="bodyHtml">input untrusted HTML</param>
        /// <param name="baseUri">URL to resolve relative URLs against</param>
        /// <param name="whitelist">white-list of permitted HTML elements</param>
        /// <returns>safe HTML</returns>
        /// <seealso cref="Cleaner.Clean(Document)"/>
        public static string Clean(string bodyHtml, string baseUri, Whitelist whitelist)
        {
            Document dirty = ParseBodyFragment(bodyHtml, baseUri);
            Cleaner cleaner = new Cleaner(whitelist);
            Document clean = cleaner.Clean(dirty);
            return clean.Body.Html();
        }

        /// <summary>
        /// Get safe HTML from untrusted input HTML, by parsing input HTML and filtering it through a white-list of permitted 
        /// tags and attributes.
        /// </summary>
        /// <param name="bodyHtml">input untrusted HTML</param>
        /// <param name="whitelist">white-list of permitted HTML elements</param>
        /// <returns>safe HTML</returns>
        /// <seealso cref="Cleaner.Clean(Document)"/>
        public static string Clean(string bodyHtml, Whitelist whitelist)
        {
            return Clean(bodyHtml, string.Empty, whitelist);
        }

        /// <summary>
        /// Test if the input HTML has only tags and attributes allowed by the Whitelist. Useful for form validation. The input HTML should 
        /// still be run through the cleaner to set up enforced attributes, and to tidy the output.
        /// </summary>
        /// <param name="bodyHtml">HTML to test</param>
        /// <param name="whitelist">whitelist to test against</param>
        /// <returns>true if no tags or attributes were removed; false otherwise</returns>
        /// <seealso cref="Clean(string, NSoup.Safety.Whitelist)"/>
        public static bool IsValid(string bodyHtml, Whitelist whitelist)
        {
            Document dirty = ParseBodyFragment(bodyHtml, string.Empty);
            Cleaner cleaner = new Cleaner(whitelist);
            return cleaner.IsValid(dirty);
        }
    }
}