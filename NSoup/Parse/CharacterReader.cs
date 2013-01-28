using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NSoup.Parse
{
    /// <summary>
    /// CharacterReader consumes tokens off a string. To replace the old TokenQueue.
    /// </summary>
    public class CharacterReader
    {
        public const char EOF = (char)254;

        private readonly char[] _input;
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

            this._input = input.ToCharArray();
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
            string s = new string(_input, _pos, 1);
            _pos++;

            return s;
        }

        /// <summary>
        /// Returns the number of characters between the current position and the next instance of the input char
        /// </summary>
        /// <param name="c">Scan target</param>
        /// <returns>Offset between current position and next instance of target. -1 if not found</returns>
        public int NextIndexOf(char c)
        {
            // doesn't handle scanning for surrogates
            for (int i = _pos; i < _length; i++)
            {
                if (c == _input[i])
                    return i - _pos;
            }
            return -1;
        }

        /// <summary>
        /// Returns the number of characters between the current position and the next instance of the input sequence
        /// </summary>
        /// <param name="seq">Scan target</param>
        /// <returns>Offset between current position and next instance of target. -1 if not found.</returns>
        public int NextIndexOf(string seq)
        {
            // doesn't handle scanning for surrogates
            char startChar = seq[0];
            for (int offset = _pos; offset < _length; offset++)
            {
                // scan to first instance of startchar:
                if (startChar != _input[offset])
                {
                    while (++offset < _length && startChar != _input[offset]);
                }
                if (offset < _length)
                {
                    int i = offset + 1;
                    int last = i + seq.Length - 1;
                    
                    for (int j = 1; i < last && seq[j] == _input[i]; i++, j++) ;
                    
                    if (i == last) // found full sequence
                    {
                        return offset - _pos;
                    }
                }
            }
            return -1;
        }

        public string ConsumeTo(char c)
        {
            int offset = NextIndexOf(c);
            if (offset != -1)
            {
                string consumed = new string(_input, _pos, offset);
                _pos += offset;
                return consumed;
            }
            else
            {
                return ConsumeToEnd();
            }
        }

        public string ConsumeTo(string seq)
        {
            int offset = NextIndexOf(seq);

            if (offset != -1)
            {
                string consumed = new string(_input, _pos, offset);
                _pos += offset;
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
            while (_pos < _length && !flag)
            {
                for (int i = 0; i < seq.Length; i++)
                {
                    if (_input[_pos] == seq[i])
                    {
                        flag = true; // Break outer loop.
                        _pos--; // Nullify next pos++ operation.
                        break;
                    }
                }
                _pos++;
            }

            return _pos > start ? new string(_input, start, _pos - start) : string.Empty;
        }

        public string ConsumeToEnd()
        {
            string data = new string(_input, _pos, _length - _pos);
            _pos = _length;
            return data;
        }

        public string ConsumeLetterSequence()
        {
            int start = _pos;
            while (_pos < _length)
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

            return new string(_input, start, _pos - start);
        }

        public string ConsumeLetterThenDigitSequence()
        {
            int start = _pos;
            while (_pos < _length)
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

            return new string(_input, start, _pos - start);
        }

        public string ConsumeHexSequence()
        {
            int start = _pos;
            while (_pos < _length) {
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
            return new string(_input, start, _pos - start);
        }

        public string ConsumeDigitSequence()
        {
            int start = _pos;
            while (_pos < _length)
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
            return new string(_input, start, _pos - start);
        }

        public bool Matches(char c)
        {
            return !IsEmpty() && _input[_pos] == c;

        }

        public bool Matches(string seq)
        {
            int scanLength = seq.Length;
            if (scanLength > _length - _pos)
            {
                return false;
            }

            for (int offset = 0; offset < scanLength; offset++)
            {
                if (seq[offset] != _input[_pos + offset])
                {
                    return false;
                }
            }

            return true;
        }

        public bool MatchesIgnoreCase(string seq)
        {
            int scanLength = seq.Length;
            if (scanLength > _length - _pos)
            {
                return false;
            }

            for (int offset = 0; offset < scanLength; offset++)
            {
                char upScan = char.ToUpperInvariant(seq[offset]);
                char upTarget = char.ToUpperInvariant(_input[_pos + offset]);
                if (upScan != upTarget)
                {
                    return false;
                }
            }
            return true;
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
            string loScan = seq.ToLower(CultureInfo.CreateSpecificCulture("en-US"));
            string hiScan = seq.ToUpper(CultureInfo.CreateSpecificCulture("en-US"));
            return (NextIndexOf(loScan) > -1) || (NextIndexOf(hiScan) > -1);
        }


        public override String ToString()
        {
            return new string(_input, _pos, _length - _pos);
        }
    }
}
