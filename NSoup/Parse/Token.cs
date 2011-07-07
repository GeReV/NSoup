using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Parse
{
/// <summary>
/// Parse tokens for the Tokeniser.
/// </summary>
    internal abstract class Token
    {
        TokenType _type;

        private Token()
        {
        }

        internal class Doctype : Token
        {
            readonly StringBuilder _name = new StringBuilder();
            readonly StringBuilder _publicIdentifier = new StringBuilder();
            readonly StringBuilder _systemIdentifier = new StringBuilder();
            bool _forceQuirks = false;

            public Doctype()
            {
                _type = TokenType.Doctype;
            }

            public StringBuilder Name
            {
                get { return _name; }
            }

            public StringBuilder PublicIdentifier
            {
                get { return _publicIdentifier; }
            }

            public StringBuilder SystemIdentifier
            {
                get { return _systemIdentifier; }
            }

            public bool ForceQuirks
            {
                get { return _forceQuirks; }
                set { _forceQuirks = value; }
            }
        }

        internal abstract class Tag : Token
        {
            protected string _tagName;
            private string _pendingAttributeName;
            private string _pendingAttributeValue;

            private bool _selfClosing = false;
            protected Attributes _attributes = new Attributes(); // todo: allow nodes to not have attributes

            public void NewAttribute()
            {
                if (_pendingAttributeName != null)
                {
                    if (_pendingAttributeValue == null)
                    {
                        _pendingAttributeValue = string.Empty;
                    }

                    NSoup.Nodes.Attribute attribute = new NSoup.Nodes.Attribute(_pendingAttributeName, _pendingAttributeValue);
                    _attributes.Add(attribute);
                }
                _pendingAttributeName = null;
                _pendingAttributeValue = null;
            }

            public void FinaliseTag()
            {
                // finalises for emit
                if (_pendingAttributeName != null)
                {
                    // todo: check if attribute name exists; if so, drop and error
                    NewAttribute();
                }
            }

            public string Name()
            {
                if (_tagName.Length == 0)
                {
                    throw new InvalidOperationException("tagName is empty.");
                }
                return _tagName;
            }

            public Tag Name(string name)
            {
                _tagName = name;
                return this;
            }

            public bool IsSelfClosing
            {
                get { return _selfClosing; }
                set { _selfClosing = value; }
            }

            public Attributes Attributes
            {
                get { return _attributes; }
            }

            // these appenders are rarely hit in not null state-- caused by null chars.
            public void AppendTagName(string append)
            {
                _tagName = _tagName == null ? append : string.Concat(_tagName, append);
            }

            public void AppendTagName(char append)
            {
                AppendTagName(append.ToString());
            }

            public void AppendAttributeName(string append)
            {
                _pendingAttributeName = _pendingAttributeName == null ? append : string.Concat(_pendingAttributeName, append);
            }

            public void AppendAttributeName(char append)
            {
                AppendAttributeName(append.ToString());
            }

            public void AppendAttributeValue(string append)
            {
                _pendingAttributeValue = _pendingAttributeValue == null ? append : string.Concat(_pendingAttributeValue, append);
            }

            public void AppendAttributeValue(char append)
            {
                AppendAttributeValue(append.ToString());
            }
        }

        internal class StartTag : Tag
        {
            public StartTag()
                : base()
            {
                _type = TokenType.StartTag;
            }

            public StartTag(string name)
                : this()
            {
                this._tagName = name;
            }

            public StartTag(string name, Attributes attributes)
                : this()
            {
                this._tagName = name;
                this._attributes = attributes;
            }

            public override string ToString()
            {
                return string.Format("<{0} {1}>", Name(), Attributes.ToString());
            }
        }

        internal class EndTag : Tag
        {
            public EndTag()
                : base()
            {
                _type = TokenType.EndTag;
            }

            public EndTag(string name)
                : this()
            {
                this._tagName = name;
            }

            public override string ToString()
            {
                return string.Format("</{0} {1}>", Name(), Attributes.ToString());
            }
        }

        internal class Comment : Token
        {
            private readonly StringBuilder data = new StringBuilder();

            public Comment()
            {
                _type = TokenType.Comment;
            }

            public StringBuilder Data
            {
                get { return data; }
            }

            public override string ToString()
            {
                return string.Format("<!--{0}-->", data);
            }
        }

        internal class Character : Token
        {
            private readonly string data;

            public Character(string data)
            {
                _type = TokenType.Character;
                this.data = data;
            }

            public string Data
            {
                get { return data; }
            }

            public override string ToString()
            {
                return Data;
            }
        }

        internal class EOF : Token
        {
            public EOF()
            {
                _type = Token.TokenType.EOF;
            }
        }

        public bool IsDoctype()
        {
            return _type == TokenType.Doctype;
        }

        public Doctype AsDoctype()
        {
            return (Doctype)this;
        }

        public bool IsStartTag()
        {
            return _type == TokenType.StartTag;
        }

        public StartTag AsStartTag()
        {
            return (StartTag)this;
        }

        public bool IsEndTag()
        {
            return _type == TokenType.EndTag;
        }

        public EndTag AsEndTag()
        {
            return (EndTag)this;
        }

        public bool IsComment()
        {
            return _type == TokenType.Comment;
        }

        public Comment AsComment()
        {
            return (Comment)this;
        }

        public bool IsCharacter()
        {
            return _type == TokenType.Character;
        }

        public Character AsCharacter()
        {
            return (Character)this;
        }

        public bool IsEOF()
        {
            return _type == TokenType.EOF;
        }

        public TokenType Type
        {
            get { return _type; }
        }

        internal enum TokenType
        {
            Doctype,
            StartTag,
            EndTag,
            Comment,
            Character,
            EOF
        }
    }
}
