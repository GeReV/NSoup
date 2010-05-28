using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;
using NSoup.Select;
using NSoup;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Uri url = new Uri("http://www.codeplex.com");
            print("Fetching {0}...", url);

            Document doc = NSoupClient.Parse(url, 3 * 1000);
            Elements links = doc.Select("a[href]");
            Elements media = doc.Select("[src]");
            Elements imports = doc.Select("link[href]");

            print("\nMedia: ({0})", media.Count);
            foreach (Element src in media)
            {
                if (src.TagName.Equals("img"))
                    print(" * {0}: <{1}> {2}x{3} ({4})",
                            src.TagName, src.Attr("abs:src"), src.Attr("width"), src.Attr("height"),
                            trim(src.Attr("alt"), 20));
                else
                    print(" * {0}: <{1}>", src.TagName, src.Attr("abs:src"));
            }

            print("\nImports: ({0})", imports.Count);
            foreach (Element link in imports)
            {
                print(" * {0} <{1}> ({2})", link.TagName, link.Attr("abs:href"), link.Attr("rel"));
            }

            print("\nLinks: ({0})", links.Count);
            foreach (Element link in links)
            {
                print(" * a: <{0}>  ({1})", link.Attr("abs:href"), trim(link.Text, 35));
            }

            Console.ReadKey();
        }

        private static void print(string msg, params object[] args)
        {
            Console.Write(string.Format(msg, args));
        }

        private static string trim(string s, int width)
        {
            if (s.Length > width)
                return s.Substring(0, width - 1) + ".";
            else
                return s;
        }
    }
}
