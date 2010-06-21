using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Parse
{
    /// <summary>
    /// A character queue with parsing helpers.   
    /// </summary>
    /// <!--
    /// Original Author: Jonathan Hedley
    /// Ported to .NET by: Amir Grozki
    /// -->
    public class TokenQueue
    {
        private LinkedList<char> _queue;

        /// <summary>
        /// Create a new TokenQueue.
        /// </summary>
        /// <param name="data">string of data to back queue.</param>
        public TokenQueue(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _queue = new LinkedList<char>();
            char[] chars = data.ToCharArray();
            foreach (char c in chars)
            {
                _queue.AddLast(c);
            }
        }

        /// <summary>
        /// Is the queue empty?
        /// </summary>
        public bool IsEmpty
        {
            get { return _queue.Count <= 0; }
        }

        /// <summary>
        /// Retrieves but does not remove the first characater from the queue.
        /// </summary>
        /// <returns>First character, or null if empty.</returns>
        public char Peek()
        {
            return _queue.First.Value;
        }

        /// <summary>
        /// Add a character to the start of the queue (will be the next character retrieved).
        /// </summary>
        /// <param name="c">character to add</param>
        public void AddFirst(char c)
        {
            _queue.AddFirst(c);
        }

        /// <summary>
        /// Add a string to the start of the queue.
        /// </summary>
        /// <param name="seq">string to add.</param>
        public void AddFirst(string seq)
        {
            char[] chars = seq.ToCharArray();
            for (int i = chars.Length - 1; i >= 0; i--)
            {
                AddFirst(chars[i]);
            }
        }

        /// <summary>
        /// Tests if the next characters on the queue match the sequence. Case insensitive.
        /// </summary>
        /// <param name="seq">string to check queue for.</param>
        /// <returns>true if the next characters match.</returns>
        public bool Matches(string seq)
        {
            int len = seq.Length;
            if (len > _queue.Count)
            {
                return false;
            }

            List<char> chars = _queue.Take(len).ToList();
            char[] seqChars = seq.ToCharArray();
            for (int i = 0; i < len; i++)
            {
                char found = char.ToLowerInvariant(chars[i]);
                char check = char.ToLowerInvariant(seqChars[i]);
                if (!found.Equals(check))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tests if the next characters match any of the sequences.
        /// </summary>
        /// <param name="seq"></param>
        /// <returns></returns>
        public bool MatchesAny(params string[] seq)
        {
            foreach (string s in seq)
            {
                if (Matches(s))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Tests if the queue matches the sequence (as with match), and if they do, removes the matched string from the 
        /// queue.
        /// </summary>
        /// <param name="seq">string to search for, and if found, remove from queue.</param>
        /// <returns>true if found and removed, false if not found.</returns>
        public bool MatchChomp(string seq)
        {
            if (Matches(seq))
            {
                Consume(seq);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tests if queue starts with a whitespace character.
        /// </summary>
        /// <returns>if starts with whitespace</returns>
        public bool MatchesWhitespace()
        {
            return (_queue.Count > 0) && char.IsWhiteSpace(_queue.First.Value);
        }

        /// <summary>
        /// Test if the queue matches a word character (letter or digit).
        /// </summary>
        /// <returns>if matches a word character</returns>
        public bool MatchesWord()
        {
            return (_queue.Count > 0) && char.IsLetterOrDigit(_queue.First.Value);
        }

        /// <summary>
        /// Consume one character off queue.
        /// </summary>
        /// <returns>first character on queue.</returns>
        public char Consume()
        {
            char c = _queue.First.Value;
            _queue.RemoveFirst();
            return c;
        }

        /// <summary>
        /// Consumes the supplied sequence of the queue. If the queue does not start with the supplied sequence, will 
        /// throw an illegal state exception -- but you should be running match() against that condition.
        /// </summary>
        /// <remarks>Case insensitive.</remarks>
        /// <param name="seq">sequence to remove from head of queue.</param>
        public void Consume(string seq)
        {
            if (!Matches(seq))
            {
                throw new Exception("Queue did not match expected sequence");
            }

            int len = seq.Length;
            if (len > _queue.Count)
            {
                throw new Exception("Queue not long enough to consume sequence");
            }

            for (int i = 0; i < len; i++)
            {
                Consume();
            }
        }

        /// <summary>
        /// Pulls a string off the queue, up to but exclusive of the match sequence, or to the queue running out.
        /// </summary>
        /// <param name="seq">string to end on (and not include in return, but leave on queue)</param>
        /// <returns>The matched data consumed from queue.</returns>
        public string ConsumeTo(string seq)
        {
            return ConsumeToAny(seq);
        }

        /// <summary>
        /// Consumes to the first sequence provided, or to the end of the queue. Leaves the terminator on the queue.
        /// </summary>
        /// <param name="seq">any number of terminators to consume to</param>
        /// <returns>consumed string</returns>
        public string ConsumeToAny(params string[] seq)
        {
            StringBuilder accum = new StringBuilder();
            while (_queue.Count > 0 && !MatchesAny(seq))
            {
                accum.Append(Consume());
            }

            return accum.ToString();
        }

        /// <summary>
        /// Pulls a string off the queue (like consumeTo), and then pulls off the matched string (but does not return it).
        /// </summary>
        /// <remarks>
        /// If the queue runs out of characters before finding the seq, will return as much as it can (and queue will go 
        /// isEmpty() == true).
        /// </remarks>
        /// <param name="seq">string to match up to, and not include in return, and to pull off queue</param>
        /// <returns>Data matched from queue.</returns>
        public string ChompTo(string seq)
        {
            string data = ConsumeTo(seq);
            MatchChomp(seq);
            return data;
        }

        /// <summary>
        /// Pulls the next run of whitespace characters of the queue.
        /// </summary>
        public bool ConsumeWhitespace()
        {
            bool seen = false;
            while (_queue.Count > 0 && char.IsWhiteSpace(_queue.First.Value))
            {
                Consume();
                seen = true;
            }
            return seen;
        }

        /// <summary>
        /// Retrieves the next run of word type (letter or digit) off the queue.
        /// </summary>
        /// <returns>string of word characters from queue, or empty string if none.</returns>
        public string ConsumeWord()
        {
            StringBuilder wordAccum = new StringBuilder();
            while (_queue.Count > 0 && char.IsLetterOrDigit(_queue.First.Value))
            {

                char c = _queue.First.Value;
                _queue.RemoveFirst();

                wordAccum.Append(c);
            }
            return wordAccum.ToString();
        }

        /// <summary>
        /// onsume a CSS identifier (ID or class) off the queue (letter, digit, -, _)
        /// http://www.w3.org/TR/CSS2/syndata.html#value-def-identifier     
        /// </summary>
        /// <returns>identifier</returns>
        public string ConsumeCssIdentifier()
        {
            StringBuilder accum = new StringBuilder();
            char c = _queue.First.Value;
            while (_queue.Count > 0 && (char.IsLetterOrDigit(c) || c.Equals('-') || c.Equals('_')))
            {
                c = _queue.First.Value;
                _queue.RemoveFirst();
                accum.Append(c);

                c = _queue.First.Value;
            }
            return accum.ToString();
        }

        /// <summary>
        /// Consume an attribute key off the queue (letter, digit, -, _, :")
        /// </summary>
        /// <returns>attribute key</returns>
        public string ConsumeAttributeKey()
        {
            StringBuilder accum = new StringBuilder();
            while (_queue.Count > 0 && (char.IsLetterOrDigit(_queue.First.Value) || MatchesAny("-", "_", ":")))
            {

                char c = _queue.First.Value;
                _queue.RemoveFirst();

                accum.Append(c);
            }
            return accum.ToString();
        }

        /// <summary>
        /// Consume and return whatever is left on the queue.
        /// </summary>
        /// <returns>remained of queue.</returns>
        public string Remainder()
        {
            StringBuilder accum = new StringBuilder();
            while (_queue.Count > 0)
            {
                accum.Append(Consume());
            }
            return accum.ToString();
        }

        public override string ToString()
        {
            return _queue.ToString();
        }
    }
}
