using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Parse
{
    /// <summary>
    /// A Parse Error records an error in the input HTML that occurs in either the tokenisation or the tree building phase.
    /// </summary>
    // todo: currently not ready for public consumption. revisit api, and exposure methods
    internal class ParseError
    {
        private string _errorMsg;
        private int _pos;
        private char _c;
        private TokeniserState _tokeniserState;
        private TreeBuilderState _treeBuilderState;
        private Token _token;

        public ParseError(string errorMsg, char c, TokeniserState tokeniserState, int pos)
        {
            this._errorMsg = errorMsg;
            this._c = c;
            this._tokeniserState = tokeniserState;
            this._pos = pos;
        }

        public ParseError(string errorMsg, TokeniserState tokeniserState, int pos)
        {
            this._errorMsg = errorMsg;
            this._tokeniserState = tokeniserState;
            this._pos = pos;
        }

        public ParseError(string errorMsg, int pos)
        {
            this._errorMsg = errorMsg;
            this._pos = pos;
        }

        public ParseError(string errorMsg, TreeBuilderState treeBuilderState, Token token, int pos)
        {
            this._errorMsg = errorMsg;
            this._treeBuilderState = treeBuilderState;
            this._token = token;
            this._pos = pos;
        }

        public string ErrorMessage
        {
            get { return _errorMsg; }
        }

        public int Position
        {
            get { return _pos; }
        }
    }
}
