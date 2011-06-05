using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;
using System.Net;
using NSoup.Helper;

namespace NSoup
{
    /// <summary>
    /// GET and POST http methods.
    /// </summary>
    public enum Method
    {
        Get, Post
    }

    public interface IConnection
    {
        /// <summary>
        /// Set the request URL to fetch. The protocol must be HTTP or HTTPS.
        /// </summary>
        /// <param name="url">URL to connect to</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Url(Uri url);

        /// <summary>
        /// Set the request URL to fetch. The protocol must be HTTP or HTTPS.
        /// </summary>
        /// <param name="url">URL to connect to</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Url(string url);

        /// <summary>
        /// Set the request user-agent header.
        /// </summary>
        /// <param name="userAgent">user-agent to use</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection UserAgent(string userAgent);

        /// <summary>
        /// Set the request timeouts (connect and read). If a timeout occurs, an IOException will be thrown. The default 
        /// timeout is 3 seconds (3000 millis). A timeout of zero is treated as an infinite timeout.
        /// </summary>
        /// <param name="millis">number of milliseconds (thousandths of a second) before timing out connects or reads.</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Timeout(int millis);

        /// <summary>
        /// Set the request referrer (aka "referer") header.
        /// </summary>
        /// <param name="referrer">referrer to use</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Referrer(string referrer);

        /// <summary>
        /// Configures the connection to (not) follow server redirects. By default this is <b>true</b>.
        /// </summary>
        /// <param name="followRedirects">true if server redirects should be followed.</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection FollowRedirects(bool followRedirects);

        /// <summary>
        /// Set the request method to use, GET or POST. Default is GET.
        /// </summary>
        /// <param name="method">HTTP request method</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Method(Method method);

        /// <summary>
        /// Add a request data parameter. Request parameters are sent in the request query string for GETs, and in the request 
        /// body for POSTs. A request may have multiple values of the same name.
        /// </summary>
        /// <param name="key">data key</param>
        /// <param name="value">data value</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Data(string key, string value);

        /// <summary>
        /// Adds all of the supplied data to the request data parameters
        /// </summary>
        /// <param name="data">dictionary of data parameters</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Data(IDictionary<string, string> data);

        /// <summary>
        /// Add a number of request data parameters. Multiple parameters may be set at once, e.g.: 
        /// <code>.data("name", "jsoup", "language", "Java", "language", "English");</code> creates a query string like: 
        /// <code>?name=jsoup&language=Java&language=English</code>
        /// </summary>
        /// <param name="keyvals">a set of key value pairs.</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Data(params string[] keyvals);

        /// <summary>
        /// Set a request header.
        /// </summary>
        /// <param name="name">header name</param>
        /// <param name="value">header value</param>
        /// <returns>this IConnection, for chaining</returns>
        /// <seealso cref="IConnection.Request.Headers()"/>
        IConnection Header(string name, string value);

        /// <summary>
        /// Set a cookie to be sent in the request
        /// </summary>
        /// <param name="name">name of cookie</param>
        /// <param name="value">value of cookie</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Cookie(string name, string value);

        /// <summary>
        /// Execute the request as a GET, and parse the result.
        /// </summary>
        /// <returns>parsed Document</returns>
        /// <exception cref="IOException" />
        Document Get();

        /// <summary>
        /// Execute the request as a POST, and parse the result.
        /// </summary>
        /// <returns>parsed Document</returns>
        /// <exception cref="IOException" />
        Document Post();

        /// <summary>
        /// Execute the request.
        /// </summary>
        /// <returns>a response object</returns>
        /// <exception cref="IOException" />
        IResponse Execute();

        /// <summary>
        /// Get the request object associatated with this IConnection
        /// </summary>
        /// <returns>request</returns>
        IRequest Request();

        /// <summary>
        /// Set the IConnection's request
        /// </summary>
        /// <param name="request">new request object</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Request(IRequest request);

        /// <summary>
        /// Get the response, once the request has been executed
        /// </summary>
        /// <returns>response</returns>
        IResponse Response();

        /// <summary>
        /// Set the conenction's response
        /// </summary>
        /// <param name="response">new response</param>
        /// <returns>this IConnection, for chaining</returns>
        IConnection Response(IResponse response);
    }

    /// <summary>
    /// Common methods for Requests and Responses
    /// </summary>
    /// <typeparam name="T">Type of IConnectionBase, either Request or Response</typeparam>
    public interface IConnectionBase<T> where T : IConnectionBase<T>
    {

        /// <summary>
        /// Gets the URL
        /// </summary>
        /// <returns>URL</returns>
        Uri Url();

        /// <summary>
        /// Sets the URL
        /// </summary>
        /// <param name="url">new URL</param>
        /// <returns>this, for chaining</returns>
        IConnectionBase<T> Url(Uri url);

        /// <summary>
        /// Gets the request method
        /// </summary>
        /// <returns>method</returns>
        Method Method();

        /// <summary>
        /// Sets the request method
        /// </summary>
        /// <param name="method">new method</param>
        /// <returns>this, for chaining</returns>
        IConnectionBase<T> Method(Method method);

        /// <summary>
        /// Gets the value of a header. This is a simplified header model, where a header may only have one value.
        /// Header names are case insensitive.
        /// </summary>
        /// <param name="name">name of header (case insensitive)</param>
        /// <returns>value of header, or null if not set.</returns>
        /// <see cref="HasHeader(string)"/>
        /// <see cref="Cookie(string)"/>
        string Header(string name);

        /// <summary>
        /// Sets a header. This method will overwrite any existing header with the same case insensitive name. 
        /// </summary>
        /// <param name="name">Name of header</param>
        /// <param name="value">Value of header</param>
        /// <returns>this, for chaining</returns>
        IConnectionBase<T> Header(string name, string value);

        /// <summary>
        /// Check if a header is present
        /// </summary>
        /// <param name="name">name of header (case insensitive)</param>
        /// <returns>if the header is present in this request/response</returns>
        bool HasHeader(string name);

        /// <summary>
        /// Remove a header by name
        /// </summary>
        /// <param name="name">name of header to remove (case insensitive)</param>
        /// <returns>this, for chianing</returns>
        IConnectionBase<T> RemoveHeader(string name);

        /// <summary>
        /// Retrieve all of the request/response headers as a map
        /// </summary>
        /// <returns>headers</returns>
        IDictionary<string, string> Headers();

        /// <summary>
        /// Gets a cookie value by name from this request/response.
        /// Response objects have a simplified cookie model. Each cookie set in the response is added to the response 
        /// object's cookie key=value map. The cookie's path, domain, and expiry date are ignored.
        /// </summary>
        /// <param name="name">name of cookie to retrieve.</param>
        /// <returns>value of cookie, or null if not set</returns>
        string Cookie(string name);

        /// <summary>
        /// Sets a cookie in this request/response.
        /// </summary>
        /// <param name="name">name of cookie</param>
        /// <param name="value">value of cookie</param>
        /// <returns>this, for chianing</returns>
        IConnectionBase<T> Cookie(string name, string value);

        /// <summary>
        /// Check if a cookie is present
        /// </summary>
        /// <param name="name">name of cookie</param>
        /// <returns>if the cookie is present in this request/response</returns>
        bool HasCookie(string name);

        /// <summary>
        /// Remove a cookie by name
        /// </summary>
        /// <param name="name">name of cookie to remove</param>
        /// <returns>this, for chianing</returns>
        IConnectionBase<T> RemoveCookie(string name);

        /// <summary>
        /// Retrieve all of the request/response cookies as a map
        /// </summary>
        /// <returns>cookies</returns>
        IDictionary<string, string> Cookies();

    }

    /// <summary>
    /// Represents a HTTP request.
    /// </summary>
    public interface IRequest : IConnectionBase<IRequest>
    {

        /// <summary>
        /// Gets the request timeout, in milliseconds.
        /// </summary>
        /// <returns>the timeout in milliseconds.</returns>
        int Timeout();

        /// <summary>
        /// Update the request timeout.
        /// </summary>
        /// <param name="millis">timeout, in milliseconds</param>
        /// <returns>this Request, for chaining</returns>
        IRequest Timeout(int millis);

        /// <summary>
        /// Get the current followRedirects configuration.
        /// </summary>
        /// <returns>true if followRedirects is enabled.</returns>
        bool FollowRedirects();

        /// <summary>
        /// Configures the request to (not) follow server redirects. By default this is <b>true</b>.
        /// </summary>
        /// <param name="followRedirects">true if server redirects should be followed.</param>
        /// <returns>this IConnection, for chaining</returns>
        IRequest FollowRedirects(bool followRedirects);

        /// <summary>
        /// Add a data parameter to the request
        /// </summary>
        /// <param name="keyval">data to add.</param>
        /// <returns>this Request, for chaining</returns>
        IRequest Data(KeyVal keyval);

        /// <summary>
        /// Get all of the request's data parameters
        /// </summary>
        /// <returns>collection of keyvals</returns>
        ICollection<KeyVal> Data();
    }

    /// <summary>
    /// Represents a HTTP response.
    /// </summary>
    public interface IResponse : IConnectionBase<IResponse>
    {

        /// <summary>
        /// Gets the status code of the response.
        /// </summary>
        /// <returns>status code</returns>
        HttpStatusCode StatusCode();

        /// <summary>
        /// Gets the status message of the response.
        /// </summary>
        /// <returns>status message</returns>
        string StatusMessage();

        /// <summary>
        /// Gets the character set name of the response.
        /// </summary>
        /// <returns>character set name</returns>
        string Charset();

        /// <summary>
        /// Gets the response content type (e.g. "text/html");
        /// </summary>
        /// <returns>the response content type</returns>
        string ContentType();

        /// <summary>
        /// Parse the body of the response as a Document.
        /// </summary>
        /// <returns>a parsed Document</returns>
        /// <exception cref="IOException" />
        Document Parse();

        /// <summary>
        /// Gets the body of the response as a plain string.
        /// </summary>
        /// <returns>body</returns>
        string Body();

        /// <summary>
        /// Gets the body of the response as an array of bytes.
        /// </summary>
        /// <returns>body bytes</returns>
        byte[] BodyAsBytes();

        /// <summary>
        /// Gets number of redirects.
        /// </summary>
        int NumRedirects { get; }
    }

    /// <summary>
    /// A Key Value tuple.
    /// </summary>
    public interface IKeyVal
    {

        /// <summary>
        /// Update the key of a keyval
        /// </summary>
        /// <param name="key">new key</param>
        /// <returns>this KeyVal, for chaining</returns>
        IKeyVal Key(string key);

        /// <summary>
        /// Gets the key of a keyval
        /// </summary>
        /// <returns>the key</returns>
        string Key();

        /// <summary>
        /// Update the value of a keyval
        /// </summary>
        /// <param name="value">the new value</param>
        /// <returns>this KeyVal, for chaining</returns>
        IKeyVal Value(string value);

        /// <summary>
        /// Gets the value of a keyval
        /// </summary>
        /// <returns>the value</returns>
        string Value();
    }
}