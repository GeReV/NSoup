using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Parse
{
    /// <summary>
    /// A container for ParseErrors.
    /// </summary>
    public class ParseErrorList : List<ParseError>
    {
        private static readonly int INITIAL_CAPACITY = 16;
        private readonly int maxSize;

        public ParseErrorList(int initialCapacity, int maxSize)
            : base(initialCapacity)
        {
            this.maxSize = maxSize;
        }

        public bool CanAddError
        {
            get { return this.Count < maxSize; }
        }

        public int MaxSize
        {
            get { return maxSize; }
        }

        public static ParseErrorList NoTracking()
        {
            return new ParseErrorList(0, 0);
        }

        public static ParseErrorList Tracking(int maxSize)
        {
            return new ParseErrorList(INITIAL_CAPACITY, maxSize);
        }
    }
}
