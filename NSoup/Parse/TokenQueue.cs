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
        private string _queue;
        private int _pos = 0;

        private const char ESC = '\\'; // escape char for chomp balanced.

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

            _queue = data;
        }

        /// <summary>
        /// Is the queue empty?
        /// </summary>
        public bool IsEmpty
        {
            get { return RemainingLength == 0; }
        }

        private int RemainingLength
        {
            get { return _queue.Length - _pos; }
        }

        /// <summary>
        /// Retrieves but does not remove the first character from the queue.
        /// </summary>
        /// <returns>First character, or null if empty.</returns>
        public char Peek()
        {
            // Cannot have nullable chars with functions which do not accept nulls. Hopefully '\0' would suffice.
            return IsEmpty ? '\0' : _queue[_pos];
        }

        /// <summary>
        /// Add a character to the start of the queue (will be the next character retrieved).
        /// </summary>
        /// <param name="c">character to add</param>
        public void AddFirst(char c)
        {
            AddFirst(c.ToString());
        }

        /// <summary>
        /// Add a string to the start of the queue.
        /// </summary>
        /// <param name="seq">string to add.</param>
        public void AddFirst(string seq)
        {
            // not very performant, but an edge case
            _queue = seq + _queue.Substring(_pos);
            _pos = 0;
        }

        /// <summary>
        /// Tests if the next characters on the queue match the sequence. Case insensitive.
        /// </summary>
        /// <param name="seq">string to check queue for.</param>
        /// <returns>true if the next characters match.</returns>
        public bool Matches(string seq)
        {
            int count = seq.Length;
            if (count > RemainingLength)
            {
                return false;
            }

            while (--count >= 0)
            {
                if (char.ToLowerInvariant(seq[count]) != char.ToLowerInvariant(_queue[_pos + count]))
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
            return !IsEmpty && char.IsWhiteSpace(Peek());
        }

        /// <summary>
        /// Test if the queue matches a word character (letter or digit).
        /// </summary>
        /// <returns>if matches a word character</returns>
        public bool MatchesWord()
        {
            return !IsEmpty && char.IsLetterOrDigit(Peek());
        }

        /// <summary>
        /// Consume one character off queue.
        /// </summary>
        /// <returns>first character on queue.</returns>
        public char Consume()
        {
            char c = _queue[_pos];
            _pos++;
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
            if (len > RemainingLength)
            {
                throw new Exception("Queue not long enough to consume sequence");
            }

            _pos += len;
        }

        /// <summary>
        /// Pulls a string off the queue, up to but exclusive of the match sequence, or to the queue running out.
        /// </summary>
        /// <param name="seq">string to end on (and not include in return, but leave on queue). <b>Case sensitive.</b></param>
        /// <returns>The matched data consumed from queue.</returns>
        public string ConsumeTo(string seq)
        {
            int offset = _queue.IndexOf(seq, _pos);
            if (offset != -1)
            {
                string consumed = _queue.Substring(_pos, offset);
                _pos += consumed.Length;
                return consumed;
            }
            else
            {
                return Remainder();
            }
        }

        public string ConsumeToIgnoreCase(string seq)
        {
            int start = _pos;
            string first = seq.Substring(0, 1);
            bool canScan = first.ToLowerInvariant().Equals(first.ToUpperInvariant()); // if first is not cased, use index of
            while (!IsEmpty && !Matches(seq))
            {
                if (canScan)
                {
                    int skip = _queue.IndexOf(first, _pos) - _pos;
                    if (skip <= 0)
                    {
                        _pos++;
                    }
                    else if (skip < 0) // no chance of finding, grab to end
                    {
                        _pos = _queue.Length - 1;
                    }
                    else
                    {
                        _pos += skip;
                    }
                }
                else
                {
                    _pos++;
                }
            }

            string data = _queue.Substring(start, _pos);
            return data;
        }

        /// <summary>
        /// Consumes to the first sequence provided, or to the end of the queue. Leaves the terminator on the queue.
        /// </summary>
        /// <param name="seq">any number of terminators to consume to. <b>Case insensitive.</b></param>
        /// <returns>consumed string</returns>
        // TODO: method name. not good that consumeTo cares for case, and consume to any doesn't. And the only use for this
        // is is a case sensitive time...
        public string ConsumeToAny(params string[] seq)
        {
            int start = _pos;
            while (!IsEmpty && !MatchesAny(seq))
            {
                _pos++;
            }

            string data = _queue.Substring(start, _pos);
            return data;
        }

        /// <summary>
        /// Pulls a string off the queue (like consumeTo), and then pulls off the matched string (but does not return it).
        /// </summary>
        /// <remarks>
        /// If the queue runs out of characters before finding the seq, will return as much as it can (and queue will go 
        /// isEmpty() == true).
        /// </remarks>
        /// <param name="seq">string to match up to, and not include in return, and to pull off queue. <b>Case sensitive.</b></param>
        /// <returns>Data matched from queue.</returns>
        public string ChompTo(string seq)
        {
            string data = ConsumeTo(seq);
            MatchChomp(seq);
            return data;
        }

        public string ChompToIgnoreCase(string seq) {
        string data = ConsumeToIgnoreCase(seq); // case insensitive scan
        MatchChomp(seq);
        return data;
        }

        /// <summary>
        /// Pulls a balanced string off the queue. E.g. if queue is "(one (two) three) four", (,) will return "one (two) three", 
        /// and leave " four" on the queue. Unbalanced openers and closers can be escaped (with \). Those escapes will be left 
        /// in the returned string, which is suitable for regexes (where we need to preserve the escape), but unsuitable for 
        /// contains text strings; use unescape for that. 
        /// </summary>
        /// <param name="open">opener</param>
        /// <param name="close">closer</param>
        /// <returns>data matched from the queue</returns>
        public string ChompBalanced(char open, char close)
        {
            StringBuilder accum = new StringBuilder();
            int depth = 0;
            char last = '\0';

            do
            {
                if (IsEmpty) break;
                char c = Consume();
                if (last == '\0' || !last.Equals(ESC))
                {
                    if (c.Equals(open))
                    {
                        depth++;
                    }
                    else if (c.Equals(close))
                    {
                        depth--;
                    }
                }

                if (depth > 0 && last != '\0')
                {
                    accum.Append(c); // don't include the outer match pair in the return
                }
                last = c;
            } while (depth > 0);
            return accum.ToString();
        }
    
        /// <summary>
        /// Unescaped a \ escaped string.
        /// </summary>
        /// <param name="input">backslash escaped string</param>
        /// <returns>unescaped string</returns>
        public static string Unescape(string input)
        {
            StringBuilder output = new StringBuilder();
            char last = '\0';
            foreach (char c in input.ToCharArray())
            {
                if (c.Equals(ESC))
                {
                    if (last != '\0' && last.Equals(ESC))
                    {
                        output.Append(c);
                    }
                }
                else
                {
                    output.Append(c);
                }
                last = c;
            }
            return output.ToString();
        }

        /// <summary>
        /// Pulls the next run of whitespace characters of the queue.
        /// </summary>
        public bool ConsumeWhitespace()
        {
            bool seen = false;
            while (MatchesWhitespace())
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
            int start = _pos;
            while (MatchesWord())
            {
                _pos++;
            }
            return _queue.Substring(start, _pos);
        }

        /// <summary>
        /// onsume a CSS identifier (ID or class) off the queue (letter, digit, -, _)
        /// http://www.w3.org/TR/CSS2/syndata.html#value-def-identifier     
        /// </summary>
        /// <returns>identifier</returns>
        public string ConsumeCssIdentifier()
        {
            StringBuilder accum = new StringBuilder();
            char c = Peek();
            while (!IsEmpty && char.IsLetterOrDigit(c) || c.Equals('-') || c.Equals('_'))
            {
                accum.Append(Consume());
                c = Peek();
            }
            return accum.ToString();
        }

        /// <summary>
        /// Consume an attribute key off the queue (letter, digit, -, _, :")
        /// </summary>
        /// <returns>attribute key</returns>
        public string ConsumeAttributeKey()
        {
            int start = _pos;
            while (!IsEmpty && (MatchesWord() || MatchesAny("-", "_", ":")))
            {
                _pos++;
            }

            return _queue.Substring(start, _pos);
        }

        /// <summary>
        /// Consume and return whatever is left on the queue.
        /// </summary>
        /// <returns>remained of queue.</returns>
        public string Remainder()
        {
            StringBuilder accum = new StringBuilder();
            while (!IsEmpty)
            {
                accum.Append(Consume());
            }
            return accum.ToString();
        }

        public override string ToString()
        {
            return _queue.Substring(_pos);
        }
    }
}
