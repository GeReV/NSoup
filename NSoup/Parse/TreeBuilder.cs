using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;
using NSoup.Helper;

namespace NSoup.Parse
{
    /// <summary>
    /// HTML Tree Builder; creates a DOM from Tokens.
    /// </summary>
    internal class TreeBuilder
    {
        private CharacterReader _reader;
        private Tokeniser _tokeniser;

        private TreeBuilderState _state; // the current state
        private TreeBuilderState _originalState; // original / marked state
        private Document _doc; // current doc we are building into
        private DescendableLinkedList<Element> _stack; // the stack of open elements

        private string _baseUri; // current base uri, for creating new elements
        private Token _currentToken; // currentToken is used only for error tracking.
        private Element _headElement; // the current head element
        private Element _formElement; // the current form element
        private Element _contextElement; // fragment parse context -- could be null even if fragment parsing
        private DescendableLinkedList<Element> _formattingElements = new DescendableLinkedList<Element>(); // active (open) formatting elements
        private List<Token.Character> _pendingTableCharacters = new List<Token.Character>(); // chars in table to be shifted out

        private bool _framesetOk = true; // if ok to go into frameset
        private bool _fosterInserts = false; // if next inserts should be fostered
        private bool _fragmentParsing = false; // if parsing a fragment of html

        private bool _trackErrors = false;
        private List<ParseError> _errors = new List<ParseError>();

        public TreeBuilder()
        {
        }

        private void InitialiseParse(string input, string baseUri)
        {
            _doc = new Document(baseUri);
            _reader = new CharacterReader(input);
            _tokeniser = new Tokeniser(_reader);
            _stack = new DescendableLinkedList<Element>();
            this._baseUri = baseUri;
        }

        public Document Parse(string input, string baseUri)
        {
            _state = TreeBuilderState.Initial;

            InitialiseParse(input, baseUri);

            RunParser();

            return _doc;
        }

        public IList<Node> ParseFragment(string inputFragment, Element context, string baseUri)
        {
            // context may be null
            InitialiseParse(inputFragment, baseUri);
            _contextElement = context;
            _fragmentParsing = true;
            Element root = null;

            if (context != null)
            {
                if (context.OwnerDocument != null) // quirks setup:
                {
                    _doc.QuirksMode(context.OwnerDocument.QuirksMode());
                }

                // initialise the tokeniser state:
                string contextTag = context.TagName();
                if (StringUtil.In(contextTag, "title", "textarea"))
                {
                    _tokeniser.Transition(TokeniserState.RcData);
                }
                else if (StringUtil.In(contextTag, "iframe", "noembed", "noframes", "style", "xmp"))
                {
                    _tokeniser.Transition(TokeniserState.RawText);
                }
                else if (contextTag.Equals("script"))
                {
                    _tokeniser.Transition(TokeniserState.ScriptData);
                }
                else if (contextTag.Equals(("noscript")))
                {
                    _tokeniser.Transition(TokeniserState.Data); // if scripting enabled, rawtext
                }
                else if (contextTag.Equals("plaintext"))
                {
                    _tokeniser.Transition(TokeniserState.Data);
                }
                else
                {
                    _tokeniser.Transition(TokeniserState.Data); // default
                }

                root = new Element(Tag.ValueOf("html"), baseUri);
                _doc.AppendChild(root);
                _stack.AddFirst(root);
                ResetInsertionMode();
                // todo: setup form element to nearest form on context (up ancestor chain)
            }

            RunParser();
            if (context != null)
            {
                return root.ChildNodes;
            }
            else
            {
                return _doc.ChildNodes;
            }
        }

        private void RunParser()
        {
            while (true)
            {
                // todo: handle foreign content checks

                Token token = _tokeniser.Read();
                Process(token);

                if (token.Type == Token.TokenType.EOF)
                {
                    break;
                }
            }
        }

        public bool Process(Token token)
        {
            _currentToken = token;
            return this._state.Process(token, this);
        }

        public bool Process(Token token, TreeBuilderState state)
        {
            _currentToken = token;
            return state.Process(token, this);
        }

        public void Transition(TreeBuilderState state)
        {
            this._state = state;
        }

        public TreeBuilderState State
        {
            get { return _state; }
        }

        public void MarkInsertionMode()
        {
            _originalState = _state;
        }

        public TreeBuilderState OriginalState
        {
            get { return _originalState; }
        }

        public void FramesetOk(bool framesetOk)
        {
            this._framesetOk = framesetOk;
        }

        public bool FramesetOk()
        {
            return _framesetOk;
        }

        public Element CurrentElement
        {
            get { return _stack.Last(); }
        }

        public Document Document
        {
            get { return _doc; }
        }

        public string BaseUri
        {
            get { return _baseUri; }
        }

        public void SetBaseUri(Element baseEl)
        {
            string href = baseEl.AbsUrl("href");
            if (href.Length != 0)
            { // ignore <base target> etc
                _baseUri = href;
                _doc.BaseUri = href; // set on the doc so doc.createElement(Tag) will get updated base
            }
        }

        public bool IsFragmentParsing()
        {
            return _fragmentParsing;
        }

        public void Error(TreeBuilderState state)
        {
            if (_trackErrors)
            {
                _errors.Add(new ParseError("Unexpected token", state, _currentToken, _reader.Position));
            }
        }

        public Element Insert(Token.StartTag startTag)
        {
            // handle empty unknown tags
            // when the spec expects an empty tag, will directly hit insertEmpty, so won't generate fake end tag.
            if (startTag.IsSelfClosing && !Tag.IsKnownTag(startTag.Name()))
            {
                Element el = InsertEmpty(startTag);
                Process(new Token.EndTag(el.TagName())); // ensure we get out of whatever state we are in
                return el;
            }

            Element element = new Element(Tag.ValueOf(startTag.Name()), _baseUri, startTag.Attributes);
            Insert(element);
            return element;
        }

        public Element Insert(string startTagName)
        {
            Element el = new Element(Tag.ValueOf(startTagName), _baseUri);
            Insert(el);
            return el;
        }

        public void Insert(Element el)
        {
            InsertNode(el);
            _stack.AddLast(el);
        }

        public Element InsertEmpty(Token.StartTag startTag)
        {
            Tag tag = Tag.ValueOf(startTag.Name());
            Element el = new Element(tag, _baseUri, startTag.Attributes);
            InsertNode(el);
            if (startTag.IsSelfClosing)
            {
                _tokeniser.AcknowledgeSelfClosingFlag();
                if (!tag.IsKnownTag()) // unknown tag, remember this is self closing for output
                {
                    tag.SetSelfClosing();
                }
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
            Node node;
            // characters in script and style go in as datanodes, not text nodes
            if (StringUtil.In(CurrentElement.TagName(), "script", "style"))
            {
                node = new DataNode(characterToken.Data.ToString(), _baseUri);
            }
            else
            {
                node = new TextNode(characterToken.Data.ToString(), _baseUri);
            }
            CurrentElement.AppendChild(node); // doesn't use insertNode, because we don't foster these; and will always have a stack.
        }

        private void InsertNode(Node node)
        {
            // if the stack hasn't been set up yet, elements (doctype, comments) go into the doc
            if (_stack.Count == 0)
            {
                _doc.AppendChild(node);
            }
            else if (IsFosterInserts)
            {
                InsertInFosterParent(node);
            }
            else
            {
                CurrentElement.AppendChild(node);
            }
        }

        public Element Pop()
        {
            // todo - dev, remove validation check
            if (_stack.Last.Value.NodeName.Equals("td") && !_state.GetType().Name.Equals("InCell"))
            {
                throw new InvalidOperationException("pop td not in cell");
            }
            if (_stack.Last.Value.NodeName.Equals("html"))
            {
                throw new InvalidOperationException("popping html!");
            }
            Element last = _stack.Last.Value;
            _stack.RemoveLast();
            return last;
        }

        public void Push(Element element)
        {
            _stack.AddFirst(element);
        }

        public DescendableLinkedList<Element> Stack
        {
            get { return _stack; }
        }

        public bool OnStack(Element el)
        {
            return IsElementInQueue(_stack, el);
        }

        private bool IsElementInQueue(DescendableLinkedList<Element> queue, Element element)
        {

            IEnumerator<Element> it = queue.GetDescendingEnumerator();

            while (it.MoveNext())
            {
                Element next = it.Current;
                if (next == element)
                {
                    return true;
                }
            }
            return false;
        }

        public Element GetFromStack(string elName)
        {
            IEnumerator<Element> it = _stack.GetDescendingEnumerator();
            while (it.MoveNext())
            {
                Element next = it.Current;
                if (next.NodeName.Equals(elName))
                {
                    return next;
                }
            }
            return null;
        }

        public bool RemoveFromStack(Element el)
        {
            return _stack.Remove(el);
        }

        public void PopStackToClose(string elName)
        {
            while (_stack.Last != null)
            {
                if (_stack.Last.Value.NodeName.Equals(elName))
                {
                    _stack.RemoveLast();
                    break;
                }
                else
                {
                    _stack.RemoveLast();
                }
            }
        }

        public void PopStackToClose(params string[] elNames)
        {
            while (_stack.Last != null)
            {
                if (StringUtil.In(_stack.Last.Value.NodeName, elNames))
                {
                    _stack.RemoveLast();
                    break;
                }
                else
                {
                    _stack.RemoveLast();
                }
            }
        }

        public void PopStackToBefore(string elName)
        {
            while (_stack.Last != null)
            {
                if (_stack.Last.Value.NodeName.Equals(elName))
                {
                    break;
                }
                else
                {
                    _stack.RemoveLast();
                }
            }
        }

        public void ClearStackToTableContext()
        {
            ClearStackToContext("table");
        }

        public void ClearStackToTableBodyContext()
        {
            ClearStackToContext("tbody", "tfoot", "thead");
        }

        public void ClearStackToTableRowContext()
        {
            ClearStackToContext("tr");
        }

        private void ClearStackToContext(params string[] nodeNames) {
            LinkedListNode<Element> node = _stack.Last;
        while (node != null) {
            Element next = node.Value;
            if (StringUtil.In(next.NodeName, nodeNames) || next.NodeName.Equals("html"))
            {
                break;
            }
            else
            {
                _stack.Remove(node);
                node = node.Previous;
            }
        }
    }

        public Element AboveOnStack(Element el)
        {
            //assert onStack(el);
            IEnumerator<Element> it = _stack.GetDescendingEnumerator();
            while (it.MoveNext())
            {
                Element next = it.Current;
                if (next == el)
                {
                    it.MoveNext();
                    return it.Current;
                }
            }
            return null;
        }

        public void InsertOnStackAfter(Element after, Element input)
        {
            _stack.AddAfter(_stack.Find(after), input);
        }

        public void ReplaceOnStack(Element output, Element input)
        {
            ReplaceInQueue(_stack, output, input);
        }

        private void ReplaceInQueue(LinkedList<Element> queue, Element output, Element input)
        {
            queue.AddAfter(queue.Find(output), input);
            queue.Remove(output);
        }

        public void ResetInsertionMode()
        {
            bool last = false;

            IEnumerator<Element> it = _stack.GetDescendingEnumerator();
            while (it.MoveNext())
            {
                Element node = it.Current;
                
                if (_stack.FindLast(node).Previous == null)
                {
                    last = true;
                    node = _contextElement;
                }

                string name = node.NodeName;

                if ("select".Equals(name))
                {
                    Transition(TreeBuilderState.InSelect);
                    break; // frag
                }
                else if (("td".Equals(name) || "td".Equals(name) && !last))
                {
                    Transition(TreeBuilderState.InCell);
                    break;
                }
                else if ("tr".Equals(name))
                {
                    Transition(TreeBuilderState.InRow);
                    break;
                }
                else if ("tbody".Equals(name) || "thead".Equals(name) || "tfoot".Equals(name))
                {
                    Transition(TreeBuilderState.InTableBody);
                    break;
                }
                else if ("caption".Equals(name))
                {
                    Transition(TreeBuilderState.InCaption);
                    break;
                }
                else if ("colgroup".Equals(name))
                {
                    Transition(TreeBuilderState.InColumnGroup);
                    break; // frag
                }
                else if ("table".Equals(name))
                {
                    Transition(TreeBuilderState.InTable);
                    break;
                }
                else if ("head".Equals(name))
                {
                    Transition(TreeBuilderState.InBody);
                    break; // frag
                }
                else if ("body".Equals(name))
                {
                    Transition(TreeBuilderState.InBody);
                    break;
                }
                else if ("frameset".Equals(name))
                {
                    Transition(TreeBuilderState.InFrameset);
                    break; // frag
                }
                else if ("html".Equals(name))
                {
                    Transition(TreeBuilderState.BeforeHead);
                    break; // frag
                }
                else if (last)
                {
                    Transition(TreeBuilderState.InBody);
                    break; // frag
                }
            }
        }

        // todo: tidy up in specific scope methods
        private bool InSpecificScope(string targetName, string[] baseTypes, string[] extraTypes)
        {
            return InSpecificScope(new string[] { targetName }, baseTypes, extraTypes);
        }

        private bool InSpecificScope(string[] targetNames, string[] baseTypes, string[] extraTypes)
        {
            IEnumerator<Element> it = _stack.GetDescendingEnumerator();
            while (it.MoveNext())
            {
                Element el = it.Current;
                string elName = el.NodeName;
                if (StringUtil.In(elName, targetNames))
                {
                    return true;
                }
                if (StringUtil.In(elName, baseTypes))
                {
                    return false;
                }
                if (extraTypes != null && StringUtil.In(elName, extraTypes))
                {
                    return false;
                }
            }

            throw new InvalidOperationException("should not be reachable.");
            //return false;
        }

        public bool InScope(string[] targetNames)
        {
            return InSpecificScope(targetNames, new string[] { "applet", "caption", "html", "table", "td", "th", "marquee", "object" }, null);
        }

        public bool InScope(string targetName)
        {
            return InScope(targetName, null);
        }

        public bool InScope(string targetName, string[] extras)
        {
            return InSpecificScope(targetName, new string[] { "applet", "caption", "html", "table", "td", "th", "marquee", "object" }, extras);
            // todo: in mathml namespace: mi, mo, mn, ms, mtext annotation-xml
            // todo: in svg namespace: forignOjbect, desc, title
        }

        public bool InListItemScope(string targetName)
        {
            return InScope(targetName, new string[] { "ol", "ul" });
        }

        public bool InButtonScope(string targetName)
        {
            return InScope(targetName, new string[] { "button" });
        }

        public bool InTableScope(string targetName)
        {
            return InSpecificScope(targetName, new string[] { "html", "table" }, null);
        }

        public bool InSelectScope(string targetName)
        {
            IEnumerator<Element> it = _stack.GetDescendingEnumerator();
            while (it.MoveNext())
            {
                Element el = it.Current;
                string elName = el.NodeName;
                if (elName.Equals(targetName))
                {
                    return true;
                }
                if (!StringUtil.In(elName, "optgroup", "option")) // all elements except
                {
                    return false;
                }
            }

            throw new InvalidOperationException("should not be reachable.");
            //return false;
        }

        public Element HeadElement
        {
            set { this._headElement = value; }
            get { return _headElement; }
        }

        public bool IsFosterInserts
        {
            get { return _fosterInserts; }
            set { this._fosterInserts = value; }
        }

        public Element FormElement
        {
            get { return _formElement; }
            set
            {
                this._formElement = value;
            }
        }

        public void NewPendingTableCharacters()
        {
            _pendingTableCharacters = new List<Token.Character>();
        }

        public List<Token.Character> PendingTableCharacters
        {
            get { return _pendingTableCharacters; }
            set { this._pendingTableCharacters = value; }
        }

        /// <summary>
        /// 11.2.5.2 Closing elements that have implied end tags
        /// When the steps below require the UA to generate implied end tags, then, while the current node is a dd element, a 
        /// dt element, an li element, an option element, an optgroup element, a p element, an rp element, or an rt element, 
        /// the UA must pop the current node off the stack of open elements.
        /// </summary>
        /// <param name="excludeTag">If a step requires the UA to generate implied end tags but lists an element to exclude from the process, then the UA must perform the above steps as if that element was not in the above list.</param>
        public void GenerateImpliedEndTags(string excludeTag)
        {
            while ((excludeTag != null && !CurrentElement.NodeName.Equals(excludeTag)) &&
                    StringUtil.In(CurrentElement.NodeName, "dd", "dt", "li", "option", "optgroup", "p", "rp", "rt"))
            {
                Pop();
            }
        }

        public void GenerateImpliedEndTags()
        {
            GenerateImpliedEndTags(null);
        }

        public bool IsSpecial(Element el)
        {
            // todo: mathml's mi, mo, mn
            // todo: svg's foreigObject, desc, title
            string name = el.NodeName;
            return StringUtil.In(name, "address", "applet", "area", "article", "aside", "base", "basefont", "bgsound",
                    "blockquote", "body", "br", "button", "caption", "center", "col", "colgroup", "command", "dd",
                    "details", "dir", "div", "dl", "dt", "embed", "fieldset", "figcaption", "figure", "footer", "form",
                    "frame", "frameset", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header", "hgroup", "hr", "html",
                    "iframe", "img", "input", "isindex", "li", "link", "listing", "marquee", "menu", "meta", "nav",
                    "noembed", "noframes", "noscript", "object", "ol", "p", "param", "plaintext", "pre", "script",
                    "section", "select", "style", "summary", "table", "tbody", "td", "textarea", "tfoot", "th", "thead",
                    "title", "tr", "ul", "wbr", "xmp");
        }

        // active formatting elements
        public void PushActiveFormattingElements(Element input)
        {
            int numSeen = 0;
            Element remove = null;
            IEnumerator<Element> iter = _formattingElements.GetDescendingEnumerator();
            while (iter.MoveNext())
            {
                Element el = iter.Current;
                if (el == null) // marker
                {
                    break;
                }

                if (IsSameFormattingElement(input, el))
                {
                    numSeen++;
                }

                if (numSeen == 3)
                {
                    remove = iter.Current;
                    break;
                }
            }

            if (numSeen == 3)
            {
                _formattingElements.Remove(remove);
            }

            _formattingElements.AddLast(input);
        }

        private bool IsSameFormattingElement(Element a, Element b)
        {
            // same if: same namespace, tag, and attributes. Element.equals only checks tag, might in future check children
            return a.NodeName.Equals(b.NodeName) &&
                // a.namespace().equals(b.namespace()) &&
                    a.Attributes.Equals(b.Attributes);
            // todo: namespaces
        }

        public void ReconstructFormattingElements()
        {
            int size = _formattingElements.Count;
            if (size == 0 || _formattingElements.Last.Value == null || OnStack(_formattingElements.Last.Value))
            {
                return;
            }

            Element entry = _formattingElements.Last.Value;
            int pos = size - 1;
            bool skip = false;
            while (true)
            {
                if (pos == 0) // step 4. if none before, skip to 8
                {
                    skip = true;
                    break;
                }
                entry = _formattingElements.ElementAt(--pos); // step 5. one earlier than entry
                if (entry == null || OnStack(entry)) // step 6 - neither marker nor on stack
                {
                    break; // jump to 8, else continue back to 4
                }
            }
            while (true)
            {
                if (!skip) // step 7: on later than entry
                {
                    entry = _formattingElements.ElementAt(++pos);
                }

                if (entry == null)
                {
                    throw new InvalidOperationException("entry is null."); // should not occur, as we break at last element
                }

                // 8. create new element from element, 9 insert into current node, onto stack
                skip = false; // can only skip increment from 4.
                Element newEl = Insert(entry.NodeName); // todo: avoid fostering here?
                // newEl.namespace(entry.namespace()); // todo: namespaces
                newEl.Attributes.AddRange(entry.Attributes);

                // 10. replace entry with new entry
                _formattingElements.AddBefore(_formattingElements.Find(entry), newEl);
                _formattingElements.Remove(entry);

                // 11
                if (pos == size - 1) // if not last entry in list, jump to 7
                { 
                    break;
                }
            }
        }

        public void ClearFormattingElementsToLastMarker()
        {
            while (_formattingElements.Count > 0)
            {
                Element el = _formattingElements.Last.Value;
                _formattingElements.RemoveLast();
                if (el == null)
                {
                    break;
                }
            }
        }

        public void RemoveFromActiveFormattingElements(Element el)
        {
            LinkedListNode<Element> it = _formattingElements.Last;

            while (it != null)
            {
                if (it.Value == el)
                {
                    _formattingElements.Remove(it);
                    break;
                }
                it = it.Previous;
            }
        }

        public bool IsInActiveFormattingElements(Element el)
        {
            return IsElementInQueue(_formattingElements, el);
        }

        public Element GetActiveFormattingElement(string nodeName)
        {
            IEnumerator<Element> it = _formattingElements.GetDescendingEnumerator();
            while (it.MoveNext())
            {
                Element next = it.Current;
                if (next == null) // scope marker
                {
                    break;
                }
                else if (next.NodeName.Equals(nodeName))
                {
                    return next;
                }
            }
            return null;
        }

        public void ReplaceActiveFormattingElement(Element output, Element input)
        {
            ReplaceInQueue(_formattingElements, output, input);
        }

        public void InsertMarkerToFormattingElements()
        {
            _formattingElements.AddLast(new LinkedListNode<Element>(null));
        }

        public void InsertInFosterParent(Node input)
        {
            Element fosterParent = null;
            Element lastTable = GetFromStack("table");

            bool isLastTableParent = false;

            if (lastTable != null)
            {
                if (lastTable.Parent != null)
                {
                    fosterParent = lastTable.Parent;
                    isLastTableParent = true;
                }
                else
                {
                    fosterParent = AboveOnStack(lastTable);
                }
            }
            else
            { // no table == frag
                fosterParent = _stack.First.Value;
            }

            if (isLastTableParent)
            {
                if (lastTable == null)
                {
                    throw new InvalidOperationException("lastTable is null.");
                }

                lastTable.Before(input);
            }
            else
            {
                fosterParent.AppendChild(input);
            }
        }

        public override string ToString()
        {
            return "TreeBuilder{" +
                       "currentToken=" + _currentToken +
                       ", state=" + _state +
                       ", currentElement=" + CurrentElement +
                       '}';
        }

        public Tokeniser Tokeniser
        {
            get { return _tokeniser; }
        }
    }
}