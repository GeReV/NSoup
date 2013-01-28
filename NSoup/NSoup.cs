using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;
using NSoup.Parse;
using System.IO;
using NSoup.Safety;
using NSoup.Helper;

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
    public static class NSoupClient
    {
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
        /// Parse HTML into a Document, using the provided Parser. You can provide an alternate parser, such as a simple XML
        /// (non-HTML) parser.
        /// </summary>
        /// <param name="html">HTML to parse.</param>
        /// <param name="baseUri">The URL where the HTML was retrieved from. Used to resolve relative URLs to absolute URLs, that occur before the HTML declares a &lt;base href&gt; tag.</param>
        /// <param name="parser">Alternate parser to use.</param>
        /// <returns>Sane HTML.</returns>
        public static Document Parse(string html, string baseUri, Parser parser)
        {
            return parser.ParseInput(html, baseUri);
        }

        /// <summary>
        /// Parse HTML into a Document. As no base URI is specified, absolute URL detection relies on the HTML including a 
        /// <code>&lt;base href&gt;</code> tag.
        /// </summary>
        /// <param name="html">HTML to parse</param>
        /// <returns>Sane HTML</returns>
        /// <seealso cref="Parse(string, string)"/>
        public static Document Parse(string html)
        {
            return Parser.Parse(html, string.Empty);
        }

        /// <summary>
        /// Creates a new IConnection to a URL. Use to fetch and parse a HTML page.
        /// Use examples:
        /// <ul>
        /// <li><code>Document doc = NSoupClient.Connect("http://example.com").UserAgent("Mozilla").Data("name", "jsoup").Get();</code></li>
        /// <li><code>Document doc = NSoupClient.Connect("http://example.com").Cookie("auth", "token").Post();</code></li>
        /// </ul>
        /// </summary>
        /// <param name="url">URL to connect to. The protocol must be <code>http</code> or <code>https</code>.</param>
        /// <returns>the connection. You can add data, cookies, and headers; set the user-agent, referrer, method; and then execute.</returns>
        public static IConnection Connect(string url)
        {
            return HttpConnection.Connect(url);
        }

        /// <summary>
        /// Parse the contents of a file as HTML.
        /// </summary>
        /// <param name="filename">stream to load HTML from</param>
        /// <param name="charsetName">(optional) character set of file contents. Set to <code>null</code> to determine from <code>http-equiv</code> meta tag, if 
        /// present, or fall back to <code>UTF-8</code> (which is often safe to do).</param> 
        /// <param name="baseUri">The URL where the HTML was retrieved from, to resolve relative links against.</param>
        /// <returns>sane HTML</returns>
        /// <remarks>Throws an exception if the file could not be found, or read, or if the charsetName is invalid.</remarks>
        public static Document Parse(Stream s, string charsetName, string baseUri)
        {
            return DataUtil.Load(s, charsetName, baseUri);
        }

        /// <summary>
        /// Parse the contents of a stream as HTML.
        /// </summary>
        /// <param name="filename">stream to load HTML from</param>
        /// <param name="charsetName">(optional) character set of file contents. Set to <code>null</code> to determine from <code>http-equiv</code> meta tag, if 
        /// present, or fall back to <code>UTF-8</code> (which is often safe to do).</param> 
        /// <returns>sane HTML</returns>
        /// <remarks>Throws an exception if the stream could not be read, or if the charsetName is invalid.</remarks>
        public static Document Parse(Stream s, string charsetName)
        {
            return Parse(s, charsetName, string.Empty);
        }

        /// <summary>
        /// Parse the contents of a file as HTML. The location of the file is used as the base URI to qualify relative URLs.    
        /// </summary>
        /// <param name="filename">file to load HTML from</param>
        /// <param name="charsetName">(optional) character set of file contents. Set to null to determine from http-equiv meta tag, if 
        /// present, or fall back to <code>UTF-8</code> (which is often safe to do).</param>
        /// <returns>sane HTML</returns>
        /// <remarks>if the file could not be found, or read, or if the charsetName is invalid.</remarks>
        /// <seealso cref="parse(string, string, string)"/>
        public static Document Parse(FileStream file, string encoding)
        {
            return DataUtil.Load(file, encoding, Path.GetDirectoryName(file.Name));
        }

        /// <summary>
        /// Read an input stream, and parse it to a Document. You can provide an alternate parser, such as a simple XML
        /// (non-HTML) parser.
        /// </summary>
        /// <param name="input">Input stream to read. Make sure to close it after parsing.</param>
        /// <param name="charsetName">(Optional) Character set of file contents. Set to null to determine from http-equiv meta tag, if
        /// present, or fall back to UTF-8 (which is often safe to do).</param>
        /// <param name="baseUri">The URL where the HTML was retrieved from, to resolve relative links against.</param>
        /// <param name="parser">Alternate parser to use.</param>
        /// <returns>Sane HTML</returns>
        /// <exception cref="IOException">If the file could not be found, or read, or if the charsetName is invalid.</exception>
        public static Document Parse(Stream input, string charsetName, string baseUri, Parser parser)
        {
            return DataUtil.Load(input, charsetName, baseUri, parser);
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
        /// Fetch a URL, and parse it as HTML. Provided for compatibility; in most cases use <code>Connect(string)</code> instead.
        /// The encoding character set is determined by the content-type header or http-equiv meta tag, or falls back to <code>UTF-8</code>.
        /// </summary>
        /// <param name="url">URL to fetch (with a GET). The protocol must be <code>http</code> or <code>https</code>.</param>
        /// <param name="timeoutMillis">Connection and read timeout, in milliseconds. If exceeded, IOException is thrown.</param>
        /// <returns>The parsed HTML.</returns>
        /// <seealso cref="Connect(string)"/>
        /// <exception cref="HttpStatusException">If the response is not OK and HTTP response errors are not ignored.</exception>
        /// <exception cref="UnsupportedMimeTypeException">If the response mime type is not supported and those errors are not ignored.</exception>
        /// <exception cref="IOException">If a connection or read error occurs.</exception>
        //@throws java.net.MalformedURLException if the request URL is not a HTTP or HTTPS URL, or is otherwise malformed
        //@throws java.net.SocketTimeoutException if the connection times out
        public static Document Parse(Uri url, int timeoutMillis)
        {
            IConnection con = HttpConnection.Connect(url);
            con.Timeout(timeoutMillis);
            return con.Get();
        }

        /// <summary>
        /// Get safe HTML from untrusted input HTML, by parsing input HTML and filtering it through a white-list of permitted 
        /// tags and attributes.
        /// </summary>
        /// <param name="bodyHtml">Input untrusted HTML (body fragment)</param>
        /// <param name="baseUri">URL to resolve relative URLs against</param>
        /// <param name="whitelist">White-list of permitted HTML elements</param>
        /// <returns>Safe HTML (body fragment)</returns>
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
        /// <param name="bodyHtml">Input untrusted HTML (body fragment)</param>
        /// <param name="whitelist">White-list of permitted HTML elements</param>
        /// <returns>Safe HTML (body fragment)</returns>
        /// <seealso cref="Cleaner.Clean(Document)"/>
        public static string Clean(string bodyHtml, Whitelist whitelist)
        {
            return Clean(bodyHtml, string.Empty, whitelist);
        }

        /// <summary>
        /// Get safe HTML from untrusted input HTML, by parsing input HTML and filtering it through a white-list of
        /// permitted tags and attributes.
        /// </summary>
        /// <param name="bodyHtml">Input untrusted HTML (body fragment)</param>
        /// <param name="baseUri">URL to resolve relative URLs against</param>
        /// <param name="whitelist">White-list of permitted HTML elements</param>
        /// <param name="outputSettings">Document output settings; use to control pretty-printing and entity escape modes</param>
        /// <returns>Safe HTML (body fragment)</returns>
        /// <see cref="Cleaner.Clean(Document)"/>
        public static string Clean(string bodyHtml, string baseUri, Whitelist whitelist, OutputSettings outputSettings)
        {
            Document dirty = ParseBodyFragment(bodyHtml, baseUri);
            Cleaner cleaner = new Cleaner(whitelist);
            Document clean = cleaner.Clean(dirty);
            clean.OutputSettings(outputSettings);
            return clean.Body.Html();
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