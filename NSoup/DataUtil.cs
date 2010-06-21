using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace NSoup
{
    /// <summary>
    /// Internal static utilities for handling data.
    /// </summary>
    internal class DataUtil
    {

        /// <summary>
        /// Loads a file to a string.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="charsetName"></param>
        /// <returns></returns>
        public static string Load(string filename, string charsetName)
        {
            string data = File.ReadAllText(filename, Encoding.GetEncoding(charsetName));
            return data;
        }

        /// <summary>
        /// Fetches a URL and gets as a string.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public static string Load(Uri url, int timeoutMilliseconds)
        {
            string protocol = url.Scheme.ToLowerInvariant();

            if (!protocol.Equals("http") && !protocol.Equals("https"))
            {
                throw new InvalidOperationException("Only http & https protocols supported");
            }

            WebClient wc = new WebClient();
            return wc.DownloadString(url);
        }
    }
}