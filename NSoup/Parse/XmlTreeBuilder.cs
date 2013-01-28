using NSoup.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Parse
{
    internal class XmlTreeBuilder : TreeBuilder
    {

        protected override void InitialiseParse(string input, string baseUri, ParseErrorList errors)
        {
            base.InitialiseParse(input, baseUri, errors);
            _stack.AddLast(_doc); // place the document onto the stack. differs from HtmlTreeBuilder (not on stack)
        }

        public override bool Process(Token token)
        {
            // start tag, end tag, doctype, comment, character, eof
            switch (token.Type)
            {
                case Token.TokenType.StartTag:
                    Insert(token.AsStartTag());
                    break;
                case Token.TokenType.EndTag:
                    PopStackToClose(token.AsEndTag());
                    break;
                case Token.TokenType.Comment:
                    Insert(token.AsComment());
                    break;
                case Token.TokenType.Character:
                    Insert(token.AsCharacter());
                    break;
                case Token.TokenType.Doctype:
                    Insert(token.AsDoctype());
                    break;
                case Token.TokenType.EOF: // could put some normalisation here if desired
                    break;
                default:
                    throw new Exception("Unexpected token type: " + token.Type);
            }
            return true;
        }

        private void InsertNode(Node node)
        {
            CurrentElement.AppendChild(node);
        }

        Element Insert(Token.StartTag startTag)
        {
            Tag tag = Tag.ValueOf(startTag.Name());
            // todo: wonder if for xml parsing, should treat all tags as unknown? because it's not html.
            Element el = new Element(tag, _baseUri, startTag.Attributes);
            InsertNode(el);
            if (startTag.IsSelfClosing)
            {
                _tokeniser.AcknowledgeSelfClosingFlag();
                if (!tag.IsKnownTag()) // unknown tag, remember this is self closing for output. see above.
                    tag.SetSelfClosing();
            }
            else
            {
                _stack.AddLast(el);
            }
            return el;
        }

        public void Insert(Token.Comment commentToken)
        {
            Comment comment = new Comment(commentToken.Data.ToString(), _baseUri);
            InsertNode(comment);
        }

        public void Insert(Token.Character characterToken)
        {
            Node node = new TextNode(characterToken.Data.ToString(), _baseUri);
            InsertNode(node);
        }

        public void Insert(Token.Doctype d)
        {
            DocumentType doctypeNode = new DocumentType(d.Name.ToString(), d.PublicIdentifier.ToString(), d.SystemIdentifier.ToString(), _baseUri);
            InsertNode(doctypeNode);
        }

        /**
         * If the stack contains an element with this tag's name, pop up the stack to remove the first occurrence. If not
         * found, skips.
         *
         * @param endTag
         */
        private void PopStackToClose(Token.EndTag endTag)
        {
            string elName = endTag.Name();
            Element firstFound = null;

            IEnumerator<Element> it = _stack.GetDescendingEnumerator();
            while (it.MoveNext())
            {
                Element next = it.Current;
                if (next.NodeName.Equals(elName))
                {
                    firstFound = next;
                    break;
                }
            }
            if (firstFound == null)
            {
                return; // not found, skip
            }

            it = _stack.GetDescendingEnumerator();
            List<Element> remove = new List<Element>();
            while (it.MoveNext())
            {
                Element next = it.Current;
                if (next == firstFound)
                {
                    remove.Add(next);
                    break;
                }
                else
                {
                    remove.Add(next);
                }
            }
            foreach (Element item in remove)
            {
                _stack.Remove(item);
            }
        }
    }
}
