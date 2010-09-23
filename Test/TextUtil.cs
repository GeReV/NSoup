using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Test
{
    /// <summary>
    /// Text utils to ease testing
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class TextUtil
    {
        public static string StripNewLines(string s)
        {
            return Regex.Replace(s, "(?:\\n\\s*)", string.Empty);
        }
    }
}
