using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NSoup.Parse
{
    /// <summary>
    /// CharacterReader cosumes tokens off a string. To replace the old TokenQueue.
    /// </summary>
    internal class CharacterReader
    {
        public const char EOF = (char)254;

        private readonly string _input;
        private readonly int _length;
        private int _pos = 0;
        private int _mark = 0;

        public CharacterReader(string input)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }

            input = Regex.Replace(input, "\r\n?", "\n"); // normalise carriage returns to newlines

            this._input = input;
            this._length = input.Length;
        }

        public int Position
        {
            get { return _pos; }
        }

        public bool IsEmpty()
        {
            return _pos >= _length;
        }

        public char Current()
        {
            return IsEmpty() ? EOF : _input[_pos];
        }

        public char Consume()
        {
            char val = IsEmpty() ? EOF : _input[_pos];
            _pos++;
            return val;
        }

        public void Unconsume()
        {
            _pos--;
        }

        public void Advance()
        {
            _pos++;
        }

        public void Mark()
        {
            _mark = _pos;
        }

        public void RewindToMark()
        {
            _pos = _mark;
        }

        public string ConsumeAsString()
        {
            string s = _input.Substring(_pos, 1);
            _pos++;

            return s;
        }

        public string ConsumeTo(char c)
        {
            int offset = _input.IndexOf(c, _pos);
            if (offset != -1)
            {
                string consumed = _input.Substring(_pos, offset - _pos);
                _pos += consumed.Length;
                return consumed;
            }
            else
            {
                return ConsumeToEnd();
            }
        }

        public string ConsumeTo(string seq)
        {
            int offset = _input.IndexOf(seq, _pos);

            if (offset != -1)
            {
                string consumed = _input.Substring(_pos, offset - _pos);
                _pos += consumed.Length;
                return consumed;
            }
            else
            {
                return ConsumeToEnd();
            }
        }

        public string ConsumeToAny(params char[] seq)
        {
            int start = _pos;

            bool flag = false;
            while (!IsEmpty() && !flag)
            {
                char c = _input[_pos];
                foreach (char seek in seq)
                {
                    if (seek == c)
                    {
                        flag = true; // Break outer loop.
                        _pos--; // Nullify next pos++ operation.
                        break;
                    }
                }
                _pos++;
            }

            return _pos > start ? _input.Substring(start, _pos - start) : string.Empty;
        }

        public string ConsumeToEnd()
        {
            string data = _input.Substring(_pos);
            _pos = _input.Length;
            return data;
        }

        public string ConsumeLetterSequence()
        {
            int start = _pos;
            while (!IsEmpty())
            {
                char c = _input[_pos];
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    _pos++;
                }
                else
                {
                    break;
                }
            }

            return _input.Substring(start, _pos - start);
        }

        public string ConsumeHexSequence()
        {
            int start = _pos;
            while (!IsEmpty())
            {
                char c = _input[_pos];
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'))
                {
                    _pos++;
                }
                else
                {
                    break;
                }
            }
            return _input.Substring(start, _pos - start);
        }

        public string ConsumeDigitSequence()
        {
            int start = _pos;
            while (!IsEmpty())
            {
                char c = _input[_pos];
                if (c >= '0' && c <= '9')
                {
                    _pos++;
                }
                else
                {
                    break;
                }
            }
            return _input.Substring(start, _pos - start);
        }

        public bool Matches(char c)
        {
            return !IsEmpty() && _input[_pos] == c;

        }

        public bool Matches(string seq)
        {
            return _input.Substring(_pos).StartsWith(seq);
        }

        public bool MatchesIgnoreCase(string seq)
        {
            if (_input.Length - _pos < seq.Length)
            {
                return false;
            }
            return _input.Substring(_pos, seq.Length).Equals(seq, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool MatchesAny(params char[] seq)
        {
            if (IsEmpty())
            {
                return false;
            }

            char c = _input[_pos];
            foreach (char seek in seq)
            {
                if (seek == c)
                {
                    return true;
                }
            }
            return false;
        }

        public bool MatchesLetter()
        {
            if (IsEmpty())
            {
                return false;
            }
            char c = _input[_pos];
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        public bool MatchesDigit()
        {
            if (IsEmpty())
            {
                return false;
            }
            char c = _input[_pos];
            return (c >= '0' && c <= '9');
        }

        public bool MatchConsume(string seq)
        {
            if (Matches(seq))
            {
                _pos += seq.Length;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool MatchConsumeIgnoreCase(string seq)
        {
            if (MatchesIgnoreCase(seq))
            {
                _pos += seq.Length;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ContainsIgnoreCase(string seq)
        {
            // used to check presence of </title>, </style>. only finds consistent case.
            string loScan = seq.ToLowerInvariant();
            string hiScan = seq.ToUpperInvariant();
            return (_input.IndexOf(loScan, _pos) > -1) || (_input.IndexOf(hiScan, _pos) > -1);
        }


        public override String ToString()
        {
            return _input.Substring(_pos);
        }
    }
}
