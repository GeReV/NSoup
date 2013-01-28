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
    public class ParseError
    {
        
        private int _pos;
        private string _errorMsg;

        public ParseError(int pos, string errorMsg)
        {
            this._pos = pos;
            this._errorMsg = errorMsg;
        }

        public ParseError(int pos, string errorFormat, params object[] args)
        {
            this._errorMsg = string.Format(errorFormat, args);
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

        public override string ToString()
        {
            return _pos + ": " + _errorMsg;
        }
    }
}
