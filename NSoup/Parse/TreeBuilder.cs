using NSoup.Helper;
using NSoup.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Parse
{
    public abstract class TreeBuilder
    {
        protected CharacterReader _reader;
        protected Tokeniser _tokeniser;
        protected Document _doc; // current doc we are building into
        protected DescendableLinkedList<Element> _stack; // the stack of open elements
        protected string _baseUri; // current base uri, for creating new elements
        protected Token _currentToken; // currentToken is used only for error tracking.
        protected ParseErrorList _errors; // null when not tracking errors

        protected virtual void InitialiseParse(string input, string baseUri, ParseErrorList errors)
        {
            if (input == null)
            {
                throw new ArgumentNullException("String input must not be null");
            }
            if (baseUri == null)
            {
                throw new ArgumentNullException("BaseURI must not be null");
            }

            _doc = new Document(baseUri);
            _reader = new CharacterReader(input);
            _errors = errors;
            _tokeniser = new Tokeniser(_reader, errors);
            _stack = new DescendableLinkedList<Element>();
            this._baseUri = baseUri;
        }

        public Document Parse(string input, string baseUri)
        {
            return Parse(input, baseUri, ParseErrorList.NoTracking());
        }

        public virtual Document Parse(string input, string baseUri, ParseErrorList errors)
        {
            InitialiseParse(input, baseUri, errors);

            RunParser();

            return _doc;
        }

        protected void RunParser()
        {
            while (true)
            {
                Token token = _tokeniser.Read();
                Process(token);

                if (token.Type == Token.TokenType.EOF)
                {
                    break;
                }
            }
        }

        public abstract bool Process(Token token);

        public Element CurrentElement
        {
            get { return _stack.Last.Value; }
        }
    }
}
