using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Parse
{

/// <summary>
/// Readers the input stream into tokens.
/// </summary>
    public class Tokeniser
    {
        public const char ReplacementChar = '\uFFFD'; // replaces null character

        private CharacterReader _reader; // html input
        private ParseErrorList _errors;

        private TokeniserState _state = TokeniserState.Data; // current tokenisation state
        private Token _emitPending; // the token we are about to emit on next read
        private bool _isEmitPending = false;
        private StringBuilder _charBuffer = new StringBuilder(); // buffers characters to output as one token
        private StringBuilder _dataBuffer; // buffers data looking for </script>

        Token.Tag _tagPending; // tag we are building up
        Token.Doctype _doctypePending; // doctype building up
        Token.Comment _commentPending; // comment building up
        private Token.StartTag _lastStartTag; // the last start tag emitted, to test appropriate end tag
        private bool _selfClosingFlagAcknowledged = true;

        public Tokeniser(CharacterReader reader, ParseErrorList errors)
        {
            this._reader = reader;
            this._errors = errors;
        }

        public Token Read()
        {
            if (!_selfClosingFlagAcknowledged)
            {
                Error("Self closing flag not acknowledged");
                _selfClosingFlagAcknowledged = true;
            }

            while (!_isEmitPending)
            {
                _state.Read(this, _reader);
            }

            // if emit is pending, a non-character token was found: return any chars in buffer, and leave token for next read:
            if (_charBuffer.Length > 0)
            {
                string str = _charBuffer.ToString();
                _charBuffer.Remove(0, _charBuffer.Length);
                return new Token.Character(str);
            }
            else
            {
                _isEmitPending = false;
                return _emitPending;
            }
        }

        public void Emit(Token token)
        {
            if (_isEmitPending)
            {
                throw new InvalidOperationException("There is an unread token pending!");
            }

            _emitPending = token;
            _isEmitPending = true;

            if (token.Type == Token.TokenType.StartTag)
            {
                Token.StartTag startTag = (Token.StartTag)token;

                _lastStartTag = startTag;

                if (startTag.IsSelfClosing)
                {
                    _selfClosingFlagAcknowledged = false;
                }
            }
            else if (token.Type == Token.TokenType.EndTag)
            {
                Token.EndTag endTag = (Token.EndTag)token;

                if (endTag.Attributes != null)
                {
                    Error("Attributes incorrectly present on end tag");
                }
            }
        }

        public void Emit(string str)
        {
            // buffer strings up until last string token found, to emit only one token for a run of character refs etc.
            // does not set isEmitPending; read checks that
            _charBuffer.Append(str);
        }

        public void Emit(char c)
        {
            _charBuffer.Append(c);
        }

        TokeniserState GetState()
        {
            return _state;
        }

        public void Transition(TokeniserState state)
        {
            this._state = state;
        }

        public void AdvanceTransition(TokeniserState state)
        {
            _reader.Advance();
            this._state = state;
        }

        public void AcknowledgeSelfClosingFlag()
        {
            _selfClosingFlagAcknowledged = true;
        }

        public char? ConsumeCharacterReference(char? additionalAllowedCharacter, bool inAttribute)
        {
            if (_reader.IsEmpty())
            {
                return null;
            }

            if (additionalAllowedCharacter != null && additionalAllowedCharacter == _reader.Current())
            {
                return null;
            }

            if (_reader.MatchesAny('\t', '\n', '\r', '\f', ' ', '<', '&'))
            {
                return null;
            }

            _reader.Mark();
            if (_reader.MatchConsume("#"))
            { // numbered
                bool isHexMode = _reader.MatchConsumeIgnoreCase("X");

                string numRef = isHexMode ? _reader.ConsumeHexSequence() : _reader.ConsumeDigitSequence();

                if (numRef.Length == 0)
                { // didn't match anything
                    CharacterReferenceError("Numeric reference with no numerals");
                    _reader.RewindToMark();
                    return null;
                }

                if (!_reader.MatchConsume(";"))
                {
                    CharacterReferenceError("Missing semicolon"); // missing semi
                }

                int charval = -1;
                try
                {
                    int numbase = isHexMode ? 16 : 10;
                    charval = Convert.ToInt32(numRef, numbase);
                }
                catch (FormatException)
                {
                } // skip
                if (charval == -1 || (charval >= 0xD800 && charval <= 0xDFFF) || charval > 0x10FFFF)
                {
                    CharacterReferenceError("Character outside of valid range");
                    return ReplacementChar;
                }
                else
                {
                    // todo: implement number replacement table
                    // todo: check for extra illegal unicode points as parse errors
                    return (char)charval;
                }
            }
            else
            { // named
                // get as many letters as possible, and look for matching entities. unconsume backwards till a match is found
                string nameRef = _reader.ConsumeLetterThenDigitSequence();
                bool looksLegit = _reader.Matches(';');

                // found if a base named entity without a ;, or an extended entity with the ;.
                bool found = (Entities.IsBaseNamedEntity(nameRef) || (Entities.IsNamedEntity(nameRef) && looksLegit));


                if (!found)
                {
                    _reader.RewindToMark();
                    if (looksLegit)
                    {
                        CharacterReferenceError(string.Format("Invalid named referenece '{0}'", nameRef));
                    }
                    return null;
                }

                if (inAttribute && (_reader.MatchesLetter() || _reader.MatchesDigit() || _reader.MatchesAny('=', '-', '_')))
                {
                    // don't want that to match
                    _reader.RewindToMark();
                    return null;
                }

                if (!_reader.MatchConsume(";"))
                {
                    CharacterReferenceError("Missing semicolon"); // missing semi
                }

                return Entities.GetCharacterByName(nameRef);
            }
        }

        public Token.Tag CreateTagPending(bool start)
        {
            if (start)
            {
                _tagPending = new Token.StartTag();
            }
            else
            {
                _tagPending = new Token.EndTag();
            }
            return _tagPending;
        }

        public void EmitTagPending()
        {
            _tagPending.FinaliseTag();
            Emit(_tagPending);
        }

        public void CreateCommentPending()
        {
            _commentPending = new Token.Comment();
        }

        public void EmitCommentPending()
        {
            Emit(_commentPending);
        }

        public void CreateDoctypePending()
        {
            _doctypePending = new Token.Doctype();
        }

        public void EmitDoctypePending()
        {
            Emit(_doctypePending);
        }

        public void CreateTempBuffer()
        {
            _dataBuffer = new StringBuilder();
        }

        public bool IsAppropriateEndTagToken()
        {
            if (_lastStartTag == null)
            {
                return false;
            }
            return _tagPending.Name().Equals(_lastStartTag.Name());
        }

        public string AppropriateEndTagName()
        {
            return _lastStartTag.Name();
        }

        public void Error(TokeniserState state)
        {
            if (_errors.CanAddError)
            {
                _errors.Add(new ParseError(_reader.Position, "Unexpected character '{0}' in input state [{1}]", _reader.Current(), state));
            }
        }

        public void EofError(TokeniserState state)
        {
            if (_errors.CanAddError)
            {
                _errors.Add(new ParseError(_reader.Position, "Unexpectedly reached end of file (EOF) in input state [{0}]", state));
            }
        }

        private void CharacterReferenceError(string message)
        {
            if (_errors.CanAddError)
            {
                _errors.Add(new ParseError(_reader.Position, "Invalid character reference: {0}", message));
            }
        }

        private void Error(string errorMsg)
        {
            if (_errors.CanAddError)
            {
                _errors.Add(new ParseError(_reader.Position, errorMsg));
            }
        }

        private bool CurrentNodeInHtmlNS()
        {
            // todo: implement namespaces correctly
            return true;
            // Element currentNode = currentNode();
            // return currentNode != null && currentNode.namespace().equals("HTML");
        }

        /// <summary>
        /// Utility method to consume reader and unescape entities found within.
        /// </summary>
        /// <param name="inAttribute"></param>
        /// <returns>Unescaped string from reader</returns>
        public string UnescapeEntities(bool inAttribute)
        {
            StringBuilder builder = new StringBuilder();
            while (!_reader.IsEmpty())
            {
                builder.Append(_reader.ConsumeTo('&'));
                if (_reader.Matches('&'))
                {
                    _reader.Consume();
                    char? c = ConsumeCharacterReference(null, inAttribute);
                    if (c == null)
                    {
                        builder.Append('&');
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
            }
            return builder.ToString();
        }

        public Token.Tag TagPending
        {
            get { return _tagPending; }
            set { _tagPending = value; }
        }

        public Token.Doctype DoctypePending
        {
            get { return _doctypePending; }
        }

        public Token.Comment CommentPending
        {
            get { return _commentPending; }
        }

        public StringBuilder DataBuffer
        {
            get { return _dataBuffer; }
        }
    }
}
