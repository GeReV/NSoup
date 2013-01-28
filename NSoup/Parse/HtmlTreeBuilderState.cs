using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;
using NSoup.Helper;

namespace NSoup.Parse
{
    /// <summary>
    /// The Tree Builder's current state. Each state embodies the processing for the state, and transitions to other states.
    /// </summary>
    internal abstract class HtmlTreeBuilderState
    {
        #region Subclasses

        protected class InitialState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (IsWhitespace(t))
                {
                    return true; // ignore whitespace
                }
                else if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                }
                else if (t.IsDoctype())
                {
                    // todo: parse error check on expected doctypes
                    // todo: quirk state check on doctype ids
                    Token.Doctype d = t.AsDoctype();
                    DocumentType doctype = new DocumentType(d.Name.ToString(), d.PublicIdentifier.ToString(), d.SystemIdentifier.ToString(), tb.BaseUri.ToString());
                    tb.Document.AppendChild(doctype);

                    if (d.ForceQuirks)
                    {
                        tb.Document.QuirksMode(Document.QuirksModeEnum.Quirks);
                    }
                    tb.Transition(BeforeHtml);
                }
                else
                {
                    // todo: check not iframe srcdoc
                    tb.Transition(BeforeHtml);
                    return tb.Process(t); // re-process token
                }
                return true;
            }
        };
        protected class BeforeHtmlState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsDoctype())
                {
                    tb.Error(this);
                    return false;
                }
                else if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                }
                else if (IsWhitespace(t))
                {
                    return true; // ignore whitespace
                }
                else if (t.IsStartTag() && t.AsStartTag().Name().Equals("html"))
                {
                    tb.Insert(t.AsStartTag());
                    tb.Transition(BeforeHead);
                }
                else if (t.IsEndTag() && (StringUtil.In(t.AsEndTag().Name(), "head", "body", "html", "br")))
                {
                    return AnythingElse(t, tb);
                }
                else if (t.IsEndTag())
                {
                    tb.Error(this);
                    return false;
                }
                else
                {
                    return AnythingElse(t, tb);
                }
                return true;
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                tb.Insert("html");
                tb.Transition(BeforeHead);
                return tb.Process(t);
            }
        };
        protected class BeforeHeadState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (IsWhitespace(t))
                {
                    return true;
                }
                else if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                }
                else if (t.IsDoctype())
                {
                    tb.Error(this);
                    return false;
                }
                else if (t.IsStartTag() && t.AsStartTag().Name().Equals("html"))
                {
                    return InBody.Process(t, tb); // does not transition
                }
                else if (t.IsStartTag() && t.AsStartTag().Name().Equals("head"))
                {
                    Element head = tb.Insert(t.AsStartTag());
                    tb.HeadElement = head;
                    tb.Transition(InHead);
                }
                else if (t.IsEndTag() && (StringUtil.In(t.AsEndTag().Name(), "head", "body", "html", "br")))
                {
                    tb.Process(new Token.StartTag("head"));
                    return tb.Process(t);
                }
                else if (t.IsEndTag())
                {
                    tb.Error(this);
                    return false;
                }
                else
                {
                    tb.Process(new Token.StartTag("head"));
                    return tb.Process(t);
                }
                return true;
            }
        };
        protected class InHeadState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (IsWhitespace(t))
                {
                    tb.Insert(t.AsCharacter());
                    return true;
                }

                switch (t.Type)
                {
                    case Token.TokenType.Comment:
                        tb.Insert(t.AsComment());
                        break;
                    case Token.TokenType.Doctype:
                        tb.Error(this);
                        return false;
                    case Token.TokenType.StartTag:
                        Token.StartTag start = t.AsStartTag();
                        string name = start.Name();
                        if (name.Equals("html"))
                        {
                            return InBody.Process(t, tb);
                        }
                        else if (StringUtil.In(name, "base", "basefont", "bgsound", "command", "link"))
                        {
                            Element el = tb.InsertEmpty(start);
                            // jsoup special: update base as it is seen
                            if (name.Equals("base") && el.HasAttr("href"))
                            {
                                tb.MaybeSetBaseUri(el);
                            }
                        }
                        else if (name.Equals("meta"))
                        {
                            Element meta = tb.InsertEmpty(start);
                            // todo: charset switches
                        }
                        else if (name.Equals("title"))
                        {
                            HandleRcData(start, tb);
                        }
                        else if (StringUtil.In(name, "noframes", "style"))
                        {
                            HandleRawText(start, tb);
                        }
                        else if (name.Equals("noscript"))
                        {
                            // else if noscript && scripting flag = true: rawtext (jsoup doesn't run script, to handle as noscript)
                            tb.Insert(start);
                            tb.Transition(InHeadNoscript);
                        }
                        else if (name.Equals("script"))
                        {
                            // skips some script rules as won't execute them
                            tb.Insert(start);
                            tb.Tokeniser.Transition(TokeniserState.ScriptData);
                            tb.MarkInsertionMode();
                            tb.Transition(Text);
                        }
                        else if (name.Equals("head"))
                        {
                            tb.Error(this);
                            return false;
                        }
                        else
                        {
                            return AnythingElse(t, tb);
                        }
                        break;
                    case Token.TokenType.EndTag:
                        Token.EndTag end = t.AsEndTag();
                        name = end.Name();
                        if (name.Equals("head"))
                        {
                            tb.Pop();
                            tb.Transition(AfterHead);
                        }
                        else if (StringUtil.In(name, "body", "html", "br"))
                        {
                            return AnythingElse(t, tb);
                        }
                        else
                        {
                            tb.Error(this);
                            return false;
                        }
                        break;
                    default:
                        return AnythingElse(t, tb);
                }
                return true;
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                tb.Process(new Token.EndTag("head"));
                return tb.Process(t);
            }
        };
        protected class InHeadNoscriptState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsDoctype())
                {
                    tb.Error(this);
                }
                else if (t.IsStartTag() && t.AsStartTag().Name().Equals("html"))
                {
                    return tb.Process(t, InBody);
                }
                else if (t.IsEndTag() && t.AsEndTag().Name().Equals("noscript"))
                {
                    tb.Pop();
                    tb.Transition(InHead);
                }
                else if (IsWhitespace(t) || t.IsComment() || (t.IsStartTag() && StringUtil.In(t.AsStartTag().Name(),
                      "basefont", "bgsound", "link", "meta", "noframes", "style")))
                {
                    return tb.Process(t, InHead);
                }
                else if (t.IsEndTag() && t.AsEndTag().Name().Equals("br"))
                {
                    return AnythingElse(t, tb);
                }
                else if ((t.IsStartTag() && StringUtil.In(t.AsStartTag().Name(), "head", "noscript")) || t.IsEndTag())
                {
                    tb.Error(this);
                    return false;
                }
                else
                {
                    return AnythingElse(t, tb);
                }
                return true;
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                tb.Error(this);
                tb.Process(new Token.EndTag("noscript"));
                return tb.Process(t);
            }
        };
        protected class AfterHeadState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (IsWhitespace(t))
                {
                    tb.Insert(t.AsCharacter());
                }
                else if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                }
                else if (t.IsDoctype())
                {
                    tb.Error(this);
                }
                else if (t.IsStartTag())
                {
                    Token.StartTag startTag = t.AsStartTag();
                    string name = startTag.Name();
                    if (name.Equals("html"))
                    {
                        return tb.Process(t, InBody);
                    }
                    else if (name.Equals("body"))
                    {
                        tb.Insert(startTag);
                        tb.FramesetOk(false);
                        tb.Transition(InBody);
                    }
                    else if (name.Equals("frameset"))
                    {
                        tb.Insert(startTag);
                        tb.Transition(InFrameset);
                    }
                    else if (StringUtil.In(name, "base", "basefont", "bgsound", "link", "meta", "noframes", "script", "style", "title"))
                    {
                        tb.Error(this);
                        Element head = tb.HeadElement;
                        tb.Push(head);
                        tb.Process(t, InHead);
                        tb.RemoveFromStack(head);
                    }
                    else if (name.Equals("head"))
                    {
                        tb.Error(this);
                        return false;
                    }
                    else
                    {
                        AnythingElse(t, tb);
                    }
                }
                else if (t.IsEndTag())
                {
                    if (StringUtil.In(t.AsEndTag().Name(), "body", "html"))
                    {
                        AnythingElse(t, tb);
                    }
                    else
                    {
                        tb.Error(this);
                        return false;
                    }
                }
                else
                {
                    AnythingElse(t, tb);
                }
                return true;
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                tb.Process(new Token.StartTag("body"));
                tb.FramesetOk(true);
                return tb.Process(t);
            }
        };
        protected class InBodyState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                switch (t.Type)
                {
                    case Token.TokenType.Character:
                        Token.Character c = t.AsCharacter();
                        if (c.Data.Equals(_nullString))
                        {
                            // todo confirm that check
                            tb.Error(this);
                            return false;
                        }
                        else if (IsWhitespace(c))
                        {
                            tb.ReconstructFormattingElements();
                            tb.Insert(c);
                        }
                        else
                        {
                            tb.ReconstructFormattingElements();
                            tb.Insert(c);
                            tb.FramesetOk(false);
                        }
                        break;
                    case Token.TokenType.Comment:
                        tb.Insert(t.AsComment());
                        break;
                    case Token.TokenType.Doctype:
                        tb.Error(this);
                        return false;
                    case Token.TokenType.StartTag:
                        Token.StartTag startTag = t.AsStartTag();
                        string name = startTag.Name();
                        if (name.Equals("html"))
                        {
                            tb.Error(this);
                            // merge attributes onto real html
                            Element html = tb.Stack.First.Value;
                            foreach (NSoup.Nodes.Attribute attribute in startTag.Attributes)
                            {
                                if (!html.HasAttr(attribute.Key))
                                {
                                    html.Attributes.Add(attribute);
                                }
                            }
                        }
                        else if (StringUtil.In(name, "base", "basefont", "bgsound", "command", "link", "meta", "noframes", "script", "style", "title"))
                        {
                            return tb.Process(t, InHead);
                        }
                        else if (name.Equals("body"))
                        {
                            tb.Error(this);
                            LinkedList<Element> stack = tb.Stack;
                            if (stack.Count == 1 || (stack.Count > 2 && !stack.ElementAt(1).NodeName.Equals("body")))
                            {
                                // only in fragment case
                                return false; // ignore
                            }
                            else
                            {
                                tb.FramesetOk(false);
                                Element body = stack.ElementAt(1);
                                foreach (NSoup.Nodes.Attribute attribute in startTag.Attributes)
                                {
                                    if (!body.HasAttr(attribute.Key))
                                    {
                                        body.Attributes.Add(attribute);
                                    }
                                }
                            }
                        }
                        else if (name.Equals("frameset"))
                        {
                            tb.Error(this);
                            LinkedList<Element> stack = tb.Stack;
                            if (stack.Count == 1 || (stack.Count > 2 && !stack.ElementAt(1).NodeName.Equals("body")))
                            {
                                // only in fragment case
                                return false; // ignore
                            }
                            else if (!tb.FramesetOk())
                            {
                                return false; // ignore frameset
                            }
                            else
                            {
                                Element second = stack.ElementAt(1);
                                if (second.Parent != null)
                                    second.Remove();
                                // pop up to html element

                                while (stack.Count > 1)
                                {
                                    stack.RemoveLast();
                                }

                                tb.Insert(startTag);
                                tb.Transition(InFrameset);
                            }
                        }
                        else if (StringUtil.In(name,
                              "address", "article", "aside", "blockquote", "center", "details", "dir", "div", "dl",
                              "fieldset", "figcaption", "figure", "footer", "header", "hgroup", "menu", "nav", "ol",
                              "p", "section", "summary", "ul"))
                        {
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            tb.Insert(startTag);
                        }
                        else if (StringUtil.In(name, "h1", "h2", "h3", "h4", "h5", "h6"))
                        {
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            if (StringUtil.In(tb.CurrentElement.NodeName, "h1", "h2", "h3", "h4", "h5", "h6"))
                            {
                                tb.Error(this);
                                tb.Pop();
                            }
                            tb.Insert(startTag);
                        }
                        else if (StringUtil.In(name, "pre", "listing"))
                        {
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            tb.Insert(startTag);
                            // todo: ignore LF if next token
                            tb.FramesetOk(false);
                        }
                        else if (name.Equals("form"))
                        {
                            if (tb.FormElement != null)
                            {
                                tb.Error(this);
                                return false;
                            }
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            Element form = tb.Insert(startTag);
                            tb.FormElement = form;
                        }
                        else if (name.Equals("li"))
                        {
                            tb.FramesetOk(false);
                            LinkedList<Element> stack = tb.Stack;
                            for (int i = stack.Count - 1; i > 0; i--)
                            {
                                Element el = stack.ElementAt(i);
                                if (el.NodeName.Equals("li"))
                                {
                                    tb.Process(new Token.EndTag("li"));
                                    break;
                                }
                                if (tb.IsSpecial(el) && !StringUtil.In(el.NodeName, "address", "div", "p"))
                                    break;
                            }
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            tb.Insert(startTag);
                        }
                        else if (StringUtil.In(name, "dd", "dt"))
                        {
                            tb.FramesetOk(false);
                            LinkedList<Element> stack = tb.Stack;
                            for (int i = stack.Count - 1; i > 0; i--)
                            {
                                Element el = stack.ElementAt(i);
                                if (StringUtil.In(el.NodeName, "dd", "dt"))
                                {
                                    tb.Process(new Token.EndTag(el.NodeName));
                                    break;
                                }

                                if (tb.IsSpecial(el) && !StringUtil.In(el.NodeName, "address", "div", "p"))
                                {
                                    break;
                                }
                            }
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            tb.Insert(startTag);
                        }
                        else if (name.Equals("plaintext"))
                        {
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            tb.Insert(startTag);
                            tb.Tokeniser.Transition(TokeniserState.PlainText); // once in, never gets out
                        }
                        else if (name.Equals("button"))
                        {
                            if (tb.InButtonScope("button"))
                            {
                                // close and reprocess
                                tb.Error(this);
                                tb.Process(new Token.EndTag("button"));
                                tb.Process(startTag);
                            }
                            else
                            {
                                tb.ReconstructFormattingElements();
                                tb.Insert(startTag);
                                tb.FramesetOk(false);
                            }
                        }
                        else if (name.Equals("a"))
                        {
                            if (tb.GetActiveFormattingElement("a") != null)
                            {
                                tb.Error(this);
                                tb.Process(new Token.EndTag("a"));

                                // still on stack?
                                Element remainingA = tb.GetFromStack("a");
                                if (remainingA != null)
                                {
                                    tb.RemoveFromActiveFormattingElements(remainingA);
                                    tb.RemoveFromStack(remainingA);
                                }
                            }
                            tb.ReconstructFormattingElements();
                            Element a = tb.Insert(startTag);
                            tb.PushActiveFormattingElements(a);
                        }
                        else if (StringUtil.In(name,
                              "b", "big", "code", "em", "font", "i", "s", "small", "strike", "strong", "tt", "u"))
                        {
                            tb.ReconstructFormattingElements();
                            Element el = tb.Insert(startTag);
                            tb.PushActiveFormattingElements(el);
                        }
                        else if (name.Equals("nobr"))
                        {
                            tb.ReconstructFormattingElements();
                            if (tb.InScope("nobr"))
                            {
                                tb.Error(this);
                                tb.Process(new Token.EndTag("nobr"));
                                tb.ReconstructFormattingElements();
                            }
                            Element el = tb.Insert(startTag);
                            tb.PushActiveFormattingElements(el);
                        }
                        else if (StringUtil.In(name, "applet", "marquee", "object"))
                        {
                            tb.ReconstructFormattingElements();
                            tb.Insert(startTag);
                            tb.InsertMarkerToFormattingElements();
                            tb.FramesetOk(false);
                        }
                        else if (name.Equals("table"))
                        {
                            if (tb.Document.QuirksMode() != Document.QuirksModeEnum.Quirks && tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            tb.Insert(startTag);
                            tb.FramesetOk(false);
                            tb.Transition(InTable);
                        }
                        else if (StringUtil.In(name, "area", "br", "embed", "img", "keygen", "wbr"))
                        {
                            tb.ReconstructFormattingElements();
                            tb.InsertEmpty(startTag);
                            tb.FramesetOk(false);
                        }
                        else if (name.Equals("input"))
                        {
                            tb.ReconstructFormattingElements();
                            Element el = tb.InsertEmpty(startTag);

                            if (!el.Attr("type").Equals("hidden", StringComparison.InvariantCultureIgnoreCase))
                            {
                                tb.FramesetOk(false);
                            }
                        }
                        else if (StringUtil.In(name, "param", "source", "track"))
                        {
                            tb.InsertEmpty(startTag);
                        }
                        else if (name.Equals("hr"))
                        {
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            tb.InsertEmpty(startTag);
                            tb.FramesetOk(false);
                        }
                        else if (name.Equals("image"))
                        {
                            // we're not supposed to ask.
                            startTag.Name("img");
                            return tb.Process(startTag);
                        }
                        else if (name.Equals("isindex"))
                        {
                            // how much do we care about the early 90s?
                            tb.Error(this);

                            if (tb.FormElement != null)
                            {
                                return false;
                            }

                            tb.Tokeniser.AcknowledgeSelfClosingFlag();
                            tb.Process(new Token.StartTag("form"));
                            if (startTag.Attributes.ContainsKey("action"))
                            {
                                Element form = tb.FormElement;
                                form.Attr("action", startTag.Attributes["action"]);
                            }
                            tb.Process(new Token.StartTag("hr"));
                            tb.Process(new Token.StartTag("label"));
                            // hope you like english.
                            string prompt = startTag.Attributes.ContainsKey("prompt") ?
                                    startTag.Attributes["prompt"] :
                                    "This is a searchable index. Enter search keywords: ";

                            tb.Process(new Token.Character(prompt));

                            // input
                            Attributes inputAttribs = new Attributes();
                            foreach (NSoup.Nodes.Attribute attr in startTag.Attributes)
                            {
                                if (!StringUtil.In(attr.Key, "name", "action", "prompt"))
                                {
                                    inputAttribs.Add(attr);
                                }
                            }
                            inputAttribs["name"] = "isindex";
                            tb.Process(new Token.StartTag("input", inputAttribs));
                            tb.Process(new Token.EndTag("label"));
                            tb.Process(new Token.StartTag("hr"));
                            tb.Process(new Token.EndTag("form"));
                        }
                        else if (name.Equals("textarea"))
                        {
                            tb.Insert(startTag);
                            // todo: If the next token is a U+000A LINE FEED (LF) character token, then ignore that token and move on to the next one. (Newlines at the start of textarea elements are ignored as an authoring convenience.)
                            tb.Tokeniser.Transition(TokeniserState.RcData);
                            tb.MarkInsertionMode();
                            tb.FramesetOk(false);
                            tb.Transition(Text);
                        }
                        else if (name.Equals("xmp"))
                        {
                            if (tb.InButtonScope("p"))
                            {
                                tb.Process(new Token.EndTag("p"));
                            }
                            tb.ReconstructFormattingElements();
                            tb.FramesetOk(false);
                            HandleRawText(startTag, tb);
                        }
                        else if (name.Equals("iframe"))
                        {
                            tb.FramesetOk(false);
                            HandleRawText(startTag, tb);
                        }
                        else if (name.Equals("noembed"))
                        {
                            // also handle noscript if script enabled
                            HandleRawText(startTag, tb);
                        }
                        else if (name.Equals("select"))
                        {
                            tb.ReconstructFormattingElements();
                            tb.Insert(startTag);
                            tb.FramesetOk(false);

                            HtmlTreeBuilderState state = tb.State;
                            if (state.Equals(InTable) || state.Equals(InCaption) || state.Equals(InTableBody) || state.Equals(InRow) || state.Equals(InCell))
                            {
                                tb.Transition(InSelectInTable);
                            }
                            else
                            {
                                tb.Transition(InSelect);
                            }
                        }
                        else if (StringUtil.In("optgroup", "option"))
                        {
                            if (tb.CurrentElement.NodeName.Equals("option"))
                            {
                                tb.Process(new Token.EndTag("option"));
                            }
                            tb.ReconstructFormattingElements();
                            tb.Insert(startTag);
                        }
                        else if (StringUtil.In("rp", "rt"))
                        {
                            if (tb.InScope("ruby"))
                            {
                                tb.GenerateImpliedEndTags();
                                if (!tb.CurrentElement.NodeName.Equals("ruby"))
                                {
                                    tb.Error(this);
                                    tb.PopStackToBefore("ruby"); // i.e. close up to but not include name
                                }
                                tb.Insert(startTag);
                            }
                        }
                        else if (name.Equals("math"))
                        {
                            tb.ReconstructFormattingElements();
                            // todo: handle A start tag whose tag name is "math" (i.e. foreign, mathml)
                            tb.Insert(startTag);
                            tb.Tokeniser.AcknowledgeSelfClosingFlag();
                        }
                        else if (name.Equals("svg"))
                        {
                            tb.ReconstructFormattingElements();
                            // todo: handle A start tag whose tag name is "svg" (xlink, svg)
                            tb.Insert(startTag);
                            tb.Tokeniser.AcknowledgeSelfClosingFlag();
                        }
                        else if (StringUtil.In(name,
                              "caption", "col", "colgroup", "frame", "head", "tbody", "td", "tfoot", "th", "thead", "tr"))
                        {
                            tb.Error(this);
                            return false;
                        }
                        else
                        {
                            tb.ReconstructFormattingElements();
                            tb.Insert(startTag);
                        }
                        break;
                    case Token.TokenType.EndTag:
                        Token.EndTag endTag = t.AsEndTag();
                        name = endTag.Name();
                        if (name.Equals("body"))
                        {
                            if (!tb.InScope("body"))
                            {
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                // todo: error if stack contains something not dd, dt, li, optgroup, option, p, rp, rt, tbody, td, tfoot, th, thead, tr, body, html
                                tb.Transition(AfterBody);
                            }
                        }
                        else if (name.Equals("html"))
                        {
                            bool notIgnored = tb.Process(new Token.EndTag("body"));
                            if (notIgnored)
                            {
                                return tb.Process(endTag);
                            }
                        }
                        else if (StringUtil.In(name,
                              "address", "article", "aside", "blockquote", "button", "center", "details", "dir", "div",
                              "dl", "fieldset", "figcaption", "figure", "footer", "header", "hgroup", "listing", "menu",
                              "nav", "ol", "pre", "section", "summary", "ul"))
                        {
                            // todo: refactor these lookups
                            if (!tb.InScope(name))
                            {
                                // nothing to close
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                tb.GenerateImpliedEndTags();
                                if (!tb.CurrentElement.NodeName.Equals(name))
                                {
                                    tb.Error(this);
                                }
                                tb.PopStackToClose(name);
                            }
                        }
                        else if (name.Equals("form"))
                        {
                            Element currentForm = tb.FormElement;
                            tb.FormElement = null;
                            if (currentForm == null || !tb.InScope(name))
                            {
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                tb.GenerateImpliedEndTags();
                                if (!tb.CurrentElement.NodeName.Equals(name))
                                {
                                    tb.Error(this);
                                }
                                // remove currentForm from stack. will shift anything under up.
                                tb.RemoveFromStack(currentForm);
                            }
                        }
                        else if (name.Equals("p"))
                        {
                            if (!tb.InButtonScope(name))
                            {
                                tb.Error(this);
                                tb.Process(new Token.StartTag(name)); // if no p to close, creates an empty <p></p>
                                return tb.Process(endTag);
                            }
                            else
                            {
                                tb.GenerateImpliedEndTags(name);
                                if (!tb.CurrentElement.NodeName.Equals(name))
                                {
                                    tb.Error(this);
                                }
                                tb.PopStackToClose(name);
                            }
                        }
                        else if (name.Equals("li"))
                        {
                            if (!tb.InListItemScope(name))
                            {
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                tb.GenerateImpliedEndTags(name);
                                if (!tb.CurrentElement.NodeName.Equals(name))
                                {
                                    tb.Error(this);
                                }
                                tb.PopStackToClose(name);
                            }
                        }
                        else if (StringUtil.In(name, "dd", "dt"))
                        {
                            if (!tb.InScope(name))
                            {
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                tb.GenerateImpliedEndTags(name);
                                if (!tb.CurrentElement.NodeName.Equals(name))
                                {
                                    tb.Error(this);
                                }
                                tb.PopStackToClose(name);
                            }
                        }
                        else if (StringUtil.In(name, "h1", "h2", "h3", "h4", "h5", "h6"))
                        {
                            if (!tb.InScope(new string[] { "h1", "h2", "h3", "h4", "h5", "h6" }))
                            {
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                tb.GenerateImpliedEndTags(name);
                                if (!tb.CurrentElement.NodeName.Equals(name))
                                {
                                    tb.Error(this);
                                }
                                tb.PopStackToClose("h1", "h2", "h3", "h4", "h5", "h6");
                            }
                        }
                        else if (name.Equals("sarcasm"))
                        {
                            // *sigh*
                            return AnyOtherEndTag(t, tb);
                        }
                        else if (StringUtil.In(name,
                              "a", "b", "big", "code", "em", "font", "i", "nobr", "s", "small", "strike", "strong", "tt", "u"))
                        {
                            // Adoption Agency Algorithm.
                            //OUTER:
                            for (int i = 0; i < 8; i++)
                            {
                                Element formatEl = tb.GetActiveFormattingElement(name);
                                if (formatEl == null)
                                {
                                    return AnyOtherEndTag(t, tb);
                                }
                                else if (!tb.OnStack(formatEl))
                                {
                                    tb.Error(this);
                                    tb.RemoveFromActiveFormattingElements(formatEl);
                                    return true;
                                }
                                else if (!tb.InScope(formatEl.NodeName))
                                {
                                    tb.Error(this);
                                    return false;
                                }
                                else if (tb.CurrentElement != formatEl)
                                {
                                    tb.Error(this);
                                }

                                Element furthestBlock = null;
                                Element commonAncestor = null;
                                bool seenFormattingElement = false;
                                LinkedList<Element> stack = tb.Stack;
                                for (int si = 0; si < stack.Count; si++)
                                {
                                    Element el = stack.ElementAt(si);
                                    if (el == formatEl)
                                    {
                                        commonAncestor = stack.ElementAt(si - 1);
                                        seenFormattingElement = true;
                                    }
                                    else if (seenFormattingElement && tb.IsSpecial(el))
                                    {
                                        furthestBlock = el;
                                        break;
                                    }
                                }

                                if (furthestBlock == null)
                                {
                                    tb.PopStackToClose(formatEl.NodeName);
                                    tb.RemoveFromActiveFormattingElements(formatEl);
                                    return true;
                                }

                                // todo: Let a bookmark note the position of the formatting element in the list of active formatting elements relative to the elements on either side of it in the list.
                                // does that mean: int pos of format el in list?
                                Element node = furthestBlock;
                                Element lastNode = furthestBlock;

                                for (int j = 0; j < 3; j++)
                                {
                                    if (tb.OnStack(node))
                                        node = tb.AboveOnStack(node);
                                    if (!tb.IsInActiveFormattingElements(node))
                                    { // note no bookmark check
                                        tb.RemoveFromStack(node);
                                        continue;
                                    }
                                    else if (node == formatEl)
                                    {
                                        break;
                                    }

                                    Element replacement = new Element(Tag.ValueOf(node.NodeName), tb.BaseUri);
                                    tb.ReplaceActiveFormattingElement(node, replacement);
                                    tb.ReplaceOnStack(node, replacement);
                                    node = replacement;

                                    if (lastNode == furthestBlock)
                                    {
                                        // todo: move the aforementioned bookmark to be immediately after the new node in the list of active formatting elements.
                                        // not getting how this bookmark both straddles the element above, but is inbetween here...
                                    }
                                    if (lastNode.Parent != null)
                                    {
                                        lastNode.Remove();
                                    }

                                    node.AppendChild(lastNode);

                                    lastNode = node;
                                }

                                if (StringUtil.In(commonAncestor.NodeName, "table", "tbody", "tfoot", "thead", "tr"))
                                {
                                    if (lastNode.Parent != null)
                                    {
                                        lastNode.Remove();
                                    }

                                    tb.InsertInFosterParent(lastNode);
                                }
                                else
                                {
                                    if (lastNode.Parent != null)
                                    {
                                        lastNode.Remove();
                                    }

                                    commonAncestor.AppendChild(lastNode);
                                }

                                Element adopter = new Element(Tag.ValueOf(name), tb.BaseUri);
                                Node[] childNodes = furthestBlock.ChildNodes.ToArray();
                                foreach (Node childNode in childNodes)
                                {
                                    adopter.AppendChild(childNode); // append will reparent. thus the clone to avvoid concurrent mod.
                                }

                                furthestBlock.AppendChild(adopter);
                                tb.RemoveFromActiveFormattingElements(formatEl);
                                // todo: insert the new element into the list of active formatting elements at the position of the aforementioned bookmark.
                                tb.RemoveFromStack(formatEl);
                                tb.InsertOnStackAfter(furthestBlock, adopter);
                            }
                        }
                        else if (StringUtil.In(name, "applet", "marquee", "object"))
                        {
                            if (!tb.InScope("name"))
                            {
                                if (!tb.InScope(name))
                                {
                                    tb.Error(this);
                                    return false;
                                }

                                tb.GenerateImpliedEndTags();

                                if (!tb.CurrentElement.NodeName.Equals(name))
                                {
                                    tb.Error(this);
                                }

                                tb.PopStackToClose(name);
                                tb.ClearFormattingElementsToLastMarker();
                            }
                        }
                        else if (name.Equals("br"))
                        {
                            tb.Error(this);
                            tb.Process(new Token.StartTag("br"));
                            return false;
                        }
                        else
                        {
                            return AnyOtherEndTag(t, tb);
                        }
                        break;
                    case Token.TokenType.EOF:
                        // todo: error if stack contains something not dd, dt, li, p, tbody, td, tfoot, th, thead, tr, body, html
                        // stop parsing
                        break;
                    default:
                        break;
                }
                return true;
            }

            bool AnyOtherEndTag(Token t, HtmlTreeBuilder tb)
            {
                string name = t.AsEndTag().Name();
                DescendableLinkedList<Element> stack = tb.Stack;
                IEnumerator<Element> it = stack.GetDescendingEnumerator();

                while (it.MoveNext())
                {
                    Element node = it.Current;
                    if (node.NodeName.Equals(name))
                    {
                        tb.GenerateImpliedEndTags(name);

                        if (!name.Equals(tb.CurrentElement.NodeName))
                        {
                            tb.Error(this);
                        }

                        tb.PopStackToClose(name);
                        break;
                    }
                    else
                    {
                        if (tb.IsSpecial(node))
                        {
                            tb.Error(this);
                            return false;
                        }
                    }
                }

                return true;
            }
        };
        protected class TextState : HtmlTreeBuilderState
        {
            // in script, style etc. normally treated as data tags
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsCharacter())
                {
                    tb.Insert(t.AsCharacter());
                }
                else if (t.IsEOF())
                {
                    tb.Error(this);
                    // if current node is script: already started
                    tb.Pop();
                    tb.Transition(tb.OriginalState);
                    return tb.Process(t);
                }
                else if (t.IsEndTag())
                {
                    // if: An end tag whose tag name is "script" -- scripting nesting level, if evaluating scripts
                    tb.Pop();
                    tb.Transition(tb.OriginalState);
                }
                return true;
            }
        };
        protected class InTableState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsCharacter())
                {
                    tb.NewPendingTableCharacters();
                    tb.MarkInsertionMode();
                    tb.Transition(InTableText);
                    return tb.Process(t);
                }
                else if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                    return true;
                }
                else if (t.IsDoctype())
                {
                    tb.Error(this);
                    return false;
                }
                else if (t.IsStartTag())
                {
                    Token.StartTag startTag = t.AsStartTag();
                    string name = startTag.Name();
                    if (name.Equals("caption"))
                    {
                        tb.ClearStackToTableContext();
                        tb.InsertMarkerToFormattingElements();
                        tb.Insert(startTag);
                        tb.Transition(InCaption);
                    }
                    else if (name.Equals("colgroup"))
                    {
                        tb.ClearStackToTableContext();
                        tb.Insert(startTag);
                        tb.Transition(InColumnGroup);
                    }
                    else if (name.Equals("col"))
                    {
                        tb.Process(new Token.StartTag("colgroup"));
                        return tb.Process(t);
                    }
                    else if (StringUtil.In(name, "tbody", "tfoot", "thead"))
                    {
                        tb.ClearStackToTableContext();
                        tb.Insert(startTag);
                        tb.Transition(InTableBody);
                    }
                    else if (StringUtil.In(name, "td", "th", "tr"))
                    {
                        tb.Process(new Token.StartTag("tbody"));
                        return tb.Process(t);
                    }
                    else if (name.Equals("table"))
                    {
                        tb.Error(this);
                        bool processed = tb.Process(new Token.EndTag("table"));
                        if (processed) // only ignored if in fragment
                        {
                            return tb.Process(t);
                        }
                    }
                    else if (StringUtil.In(name, "style", "script"))
                    {
                        return tb.Process(t, InHead);
                    }
                    else if (name.Equals("input"))
                    {
                        if (!startTag.Attributes["type"].Equals("hidden", StringComparison.InvariantCultureIgnoreCase))
                        {
                            return AnythingElse(t, tb);
                        }
                        else
                        {
                            tb.InsertEmpty(startTag);
                        }
                    }
                    else if (name.Equals("form"))
                    {
                        tb.Error(this);
                        if (tb.FormElement != null)
                            return false;
                        else
                        {
                            Element form = tb.InsertEmpty(startTag);
                            tb.FormElement = form;
                        }
                    }
                    else
                    {
                        return AnythingElse(t, tb);
                    }
                }
                else if (t.IsEndTag())
                {
                    Token.EndTag endTag = t.AsEndTag();
                    string name = endTag.Name();

                    if (name.Equals("table"))
                    {
                        if (!tb.InTableScope(name))
                        {
                            tb.Error(this);
                            return false;
                        }
                        else
                        {
                            tb.PopStackToClose("table");
                        }
                        tb.ResetInsertionMode();
                    }
                    else if (StringUtil.In(name,
                            "body", "caption", "col", "colgroup", "html", "tbody", "td", "tfoot", "th", "thead", "tr"))
                    {
                        tb.Error(this);
                        return false;
                    }
                    else
                    {
                        return AnythingElse(t, tb);
                    }
                }
                else if (t.IsEOF())
                {
                    if (tb.CurrentElement.NodeName.Equals("html"))
                        tb.Error(this);
                    return true; // stops parsing
                }
                return AnythingElse(t, tb);
            }

            bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                tb.Error(this);
                bool processed = true;
                if (StringUtil.In(tb.CurrentElement.NodeName, "table", "tbody", "tfoot", "thead", "tr"))
                {
                    tb.IsFosterInserts = true;
                    processed = tb.Process(t, InBody);
                    tb.IsFosterInserts = false;
                }
                else
                {
                    processed = tb.Process(t, InBody);
                }
                return processed;
            }
        };
        protected class InTableTextState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                switch (t.Type)
                {
                    case Token.TokenType.Character:
                        Token.Character c = t.AsCharacter();
                        if (c.Data.ToString().Equals(_nullString))
                        {
                            tb.Error(this);
                            return false;
                        }
                        else
                        {
                            tb.PendingTableCharacters.Add(c);
                        }
                        break;
                    default:
                        if (tb.PendingTableCharacters.Count > 0)
                        {
                            foreach (Token.Character character in tb.PendingTableCharacters)
                            {
                                if (!IsWhitespace(character))
                                {
                                    // InTable anything else section:
                                    tb.Error(this);
                                    if (StringUtil.In(tb.CurrentElement.NodeName, "table", "tbody", "tfoot", "thead", "tr"))
                                    {
                                        tb.IsFosterInserts = true;
                                        tb.Process(character, InBody);
                                        tb.IsFosterInserts = false;
                                    }
                                    else
                                    {
                                        tb.Process(character, InBody);
                                    }
                                }
                                else
                                {
                                    tb.Insert(character);
                                }
                            }
                            tb.NewPendingTableCharacters();
                        }
                        tb.Transition(tb.OriginalState);
                        return tb.Process(t);
                }
                return true;
            }
        };
        protected class InCaptionState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsEndTag() && t.AsEndTag().Name().Equals("caption"))
                {
                    Token.EndTag endTag = t.AsEndTag();
                    string name = endTag.Name();

                    if (!tb.InTableScope(name))
                    {
                        tb.Error(this);
                        return false;
                    }
                    else
                    {
                        tb.GenerateImpliedEndTags();
                        if (!tb.CurrentElement.NodeName.Equals("caption"))
                        {
                            tb.Error(this);
                        }
                        tb.PopStackToClose("caption");
                        tb.ClearFormattingElementsToLastMarker();
                        tb.Transition(InTable);
                    }
                }
                else if ((t.IsStartTag() &&
                    StringUtil.In(t.AsStartTag().Name(), "caption", "col", "colgroup", "tbody", "td", "tfoot", "th", "thead", "tr") ||
                    t.IsEndTag() &&
                    t.AsEndTag().Name().Equals("table")))
                {
                    tb.Error(this);
                    bool processed = tb.Process(new Token.EndTag("caption"));

                    if (processed)
                    {
                        return tb.Process(t);
                    }
                }
                else if (t.IsEndTag() && StringUtil.In(t.AsEndTag().Name(),
                      "body", "col", "colgroup", "html", "tbody", "td", "tfoot", "th", "thead", "tr"))
                {
                    tb.Error(this);
                    return false;
                }
                else
                {
                    return tb.Process(t, InBody);
                }
                return true;
            }
        };
        protected class InColumnGroupState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (IsWhitespace(t))
                {
                    tb.Insert(t.AsCharacter());
                    return true;
                }

                switch (t.Type)
                {
                    case Token.TokenType.Comment:

                        tb.Insert(t.AsComment());
                        break;
                    case Token.TokenType.Doctype:
                        tb.Error(this);
                        break;
                    case Token.TokenType.StartTag:

                        Token.StartTag startTag = t.AsStartTag();
                        String name = startTag.Name();
                        if (name.Equals("html"))
                        {
                            return tb.Process(t, InBody);
                        }
                        else if (name.Equals("col"))
                        {
                            tb.InsertEmpty(startTag);
                        }
                        else
                        {
                            return AnythingElse(t, tb);
                        }
                        break;
                    case Token.TokenType.EndTag:
                        Token.EndTag endTag = t.AsEndTag();
                        name = endTag.Name();
                        if (name.Equals("colgroup"))
                        {
                            if (tb.CurrentElement.NodeName.Equals("html"))
                            { // frag case
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                tb.Pop();
                                tb.Transition(InTable);
                            }
                        }
                        else
                        {
                            return AnythingElse(t, tb);
                        }
                        break;
                    case Token.TokenType.EOF:
                        if (tb.CurrentElement.NodeName.Equals("html"))
                        {
                            return true; // stop parsing; frag case
                        }
                        else
                        {
                            return AnythingElse(t, tb);
                        }
                    default:
                        return AnythingElse(t, tb);
                }
                return true;
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                bool processed = tb.Process(new Token.EndTag("colgroup"));
                if (processed)  // only ignored in frag case
                {
                    return tb.Process(t);
                }
                return true;
            }
        };
        protected class InTableBodyState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                switch (t.Type)
                {
                    case Token.TokenType.StartTag:
                        Token.StartTag startTag = t.AsStartTag();
                        string name = startTag.Name();
                        if (name.Equals("tr"))
                        {
                            tb.ClearStackToTableBodyContext();
                            tb.Insert(startTag);
                            tb.Transition(InRow);
                        }
                        else if (StringUtil.In(name, "th", "td"))
                        {
                            tb.Error(this);
                            tb.Process(new Token.StartTag("tr"));
                            return tb.Process(startTag);
                        }
                        else if (StringUtil.In(name, "caption", "col", "colgroup", "tbody", "tfoot", "thead"))
                        {
                            return ExitTableBody(t, tb);
                        }
                        else
                        {
                            return AnythingElse(t, tb);
                        }
                        break;
                    case Token.TokenType.EndTag:
                        Token.EndTag endTag = t.AsEndTag();
                        name = endTag.Name();
                        if (StringUtil.In(name, "tbody", "tfoot", "thead"))
                        {
                            if (!tb.InTableScope(name))
                            {
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                tb.ClearStackToTableBodyContext();
                                tb.Pop();
                                tb.Transition(InTable);
                            }
                        }
                        else if (name.Equals("table"))
                        {
                            return ExitTableBody(t, tb);
                        }
                        else if (StringUtil.In(name, "body", "caption", "col", "colgroup", "html", "td", "th", "tr"))
                        {
                            tb.Error(this);
                            return false;
                        }
                        else
                        {
                            return AnythingElse(t, tb);
                        }
                        break;
                    default:
                        return AnythingElse(t, tb);
                }
                return true;
            }

            private bool ExitTableBody(Token t, HtmlTreeBuilder tb)
            {
                if (!(tb.InTableScope("tbody") || tb.InTableScope("thead") || tb.InScope("tfoot")))
                {
                    // frag case
                    tb.Error(this);
                    return false;
                }
                tb.ClearStackToTableBodyContext();
                tb.Process(new Token.EndTag(tb.CurrentElement.NodeName)); // tbody, tfoot, thead
                return tb.Process(t);
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                return tb.Process(t, InTable);
            }
        };
        protected class InRowState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsStartTag())
                {
                    Token.StartTag startTag = t.AsStartTag();
                    String name = startTag.Name();

                    if (StringUtil.In(name, "th", "td"))
                    {
                        tb.ClearStackToTableRowContext();
                        tb.Insert(startTag);
                        tb.Transition(InCell);
                        tb.InsertMarkerToFormattingElements();
                    }
                    else if (StringUtil.In(name, "caption", "col", "colgroup", "tbody", "tfoot", "thead", "tr"))
                    {
                        return HandleMissingTr(t, tb);
                    }
                    else
                    {
                        return AnythingElse(t, tb);
                    }
                }
                else if (t.IsEndTag())
                {
                    Token.EndTag endTag = t.AsEndTag();
                    string name = endTag.Name();

                    if (name.Equals("tr"))
                    {
                        if (!tb.InTableScope(name))
                        {
                            tb.Error(this); // frag
                            return false;
                        }
                        tb.ClearStackToTableRowContext();
                        tb.Pop(); // tr
                        tb.Transition(InTableBody);
                    }
                    else if (name.Equals("table"))
                    {
                        return HandleMissingTr(t, tb);
                    }
                    else if (StringUtil.In(name, "tbody", "tfoot", "thead"))
                    {
                        if (!tb.InTableScope(name))
                        {
                            tb.Error(this);
                            return false;
                        }
                        tb.Process(new Token.EndTag("tr"));
                        return tb.Process(t);
                    }
                    else if (StringUtil.In(name, "body", "caption", "col", "colgroup", "html", "td", "th"))
                    {
                        tb.Error(this);
                        return false;
                    }
                    else
                    {
                        return AnythingElse(t, tb);
                    }
                }
                else
                {
                    return AnythingElse(t, tb);
                }
                return true;
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                return tb.Process(t, InTable);
            }

            private bool HandleMissingTr(Token t, HtmlTreeBuilder tb)
            {
                bool processed = tb.Process(new Token.EndTag("tr"));
                if (processed)
                {
                    return tb.Process(t);
                }
                else
                    return false;
            }
        };
        protected class InCellState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsEndTag())
                {
                    Token.EndTag endTag = t.AsEndTag();
                    String name = endTag.Name();

                    if (StringUtil.In(name, "td", "th"))
                    {
                        if (!tb.InTableScope(name))
                        {
                            tb.Error(this);
                            tb.Transition(InRow); // might not be in scope if empty: <td /> and processing fake end tag
                            return false;
                        }
                        tb.GenerateImpliedEndTags();
                        if (!tb.CurrentElement.NodeName.Equals(name))
                        {
                            tb.Error(this);
                        }
                        tb.PopStackToClose(name);
                        tb.ClearFormattingElementsToLastMarker();
                        tb.Transition(InRow);
                    }
                    else if (StringUtil.In(name, "body", "caption", "col", "colgroup", "html"))
                    {
                        tb.Error(this);
                        return false;
                    }
                    else if (StringUtil.In(name, "table", "tbody", "tfoot", "thead", "tr"))
                    {
                        if (!tb.InTableScope(name))
                        {
                            tb.Error(this);
                            return false;
                        }
                        CloseCell(tb);
                        return tb.Process(t);
                    }
                    else
                    {
                        return AnythingElse(t, tb);
                    }
                }
                else if (t.IsStartTag() &&
                        StringUtil.In(t.AsStartTag().Name(),
                                "caption", "col", "colgroup", "tbody", "td", "tfoot", "th", "thead", "tr"))
                {
                    if (!(tb.InTableScope("td") || tb.InTableScope("th")))
                    {
                        tb.Error(this);
                        return false;
                    }
                    CloseCell(tb);
                    return tb.Process(t);
                }
                else
                {
                    return AnythingElse(t, tb);
                }
                return true;
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                return tb.Process(t, InBody);
            }

            private void CloseCell(HtmlTreeBuilder tb)
            {
                if (tb.InTableScope("td"))
                {
                    tb.Process(new Token.EndTag("td"));
                }
                else
                {
                    tb.Process(new Token.EndTag("th")); // only here if th or td in scope
                }
            }
        };
        protected class InSelectState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                switch (t.Type)
                {
                    case Token.TokenType.Character:

                        Token.Character c = t.AsCharacter();
                        if (c.Data.ToString().Equals(_nullString))
                        {
                            tb.Error(this);
                            return false;
                        }
                        else
                        {
                            tb.Insert(c);
                        }
                        break;
                    case Token.TokenType.Comment:
                        tb.Insert(t.AsComment());
                        break;
                    case Token.TokenType.Doctype:
                        tb.Error(this);
                        return false;
                    case Token.TokenType.StartTag:
                        Token.StartTag start = t.AsStartTag();
                        string name = start.Name();
                        if (name.Equals("html"))
                            return tb.Process(start, InBody);
                        else if (name.Equals("option"))
                        {
                            tb.Process(new Token.EndTag("option"));
                            tb.Insert(start);
                        }
                        else if (name.Equals("optgroup"))
                        {
                            if (tb.CurrentElement.NodeName.Equals("option"))
                            {
                                tb.Process(new Token.EndTag("option"));
                            }
                            else if (tb.CurrentElement.NodeName.Equals("optgroup"))
                            {
                                tb.Process(new Token.EndTag("optgroup"));
                            }
                            tb.Insert(start);
                        }
                        else if (name.Equals("select"))
                        {
                            tb.Error(this);
                            return tb.Process(new Token.EndTag("select"));
                        }
                        else if (StringUtil.In(name, "input", "keygen", "textarea"))
                        {
                            tb.Error(this);

                            if (!tb.InSelectScope("select"))
                            {
                                return false; // frag
                            }

                            tb.Process(new Token.EndTag("select"));
                            return tb.Process(start);
                        }
                        else if (name.Equals("script"))
                        {
                            return tb.Process(t, InHead);
                        }
                        else
                        {
                            return AnythingElse(t, tb);
                        }
                        break;
                    case Token.TokenType.EndTag:
                        Token.EndTag end = t.AsEndTag();
                        name = end.Name();
                        if (name.Equals("optgroup"))
                        {
                            if (tb.CurrentElement.NodeName.Equals("option") && tb.AboveOnStack(tb.CurrentElement) != null && tb.AboveOnStack(tb.CurrentElement).NodeName.Equals("optgroup"))
                            {
                                tb.Process(new Token.EndTag("option"));
                            }
                            if (tb.CurrentElement.NodeName.Equals("optgroup"))
                            {
                                tb.Pop();
                            }
                            else
                            {
                                tb.Error(this);
                            }
                        }
                        else if (name.Equals("option"))
                        {
                            if (tb.CurrentElement.NodeName.Equals("option"))
                            {
                                tb.Pop();
                            }
                            else
                            {
                                tb.Error(this);
                            }
                        }
                        else if (name.Equals("select"))
                        {
                            if (!tb.InSelectScope(name))
                            {
                                tb.Error(this);
                                return false;
                            }
                            else
                            {
                                tb.PopStackToClose(name);
                                tb.ResetInsertionMode();
                            }
                        }
                        else
                        {
                            return AnythingElse(t, tb);
                        }
                        break;
                    case Token.TokenType.EOF:
                        if (!tb.CurrentElement.NodeName.Equals("html"))
                        {
                            tb.Error(this);
                        }
                        break;
                    default:
                        return AnythingElse(t, tb);
                }
                return true;
            }

            private bool AnythingElse(Token t, HtmlTreeBuilder tb)
            {
                tb.Error(this);
                return false;
            }
        };
        protected class InSelectInTableState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsStartTag() && StringUtil.In(t.AsStartTag().Name(), "caption", "table", "tbody", "tfoot", "thead", "tr", "td", "th"))
                {
                    tb.Error(this);
                    tb.Process(new Token.EndTag("select"));
                    return tb.Process(t);
                }
                else if (t.IsEndTag() && StringUtil.In(t.AsEndTag().Name(), "caption", "table", "tbody", "tfoot", "thead", "tr", "td", "th"))
                {
                    tb.Error(this);
                    if (tb.InTableScope(t.AsEndTag().Name()))
                    {
                        tb.Process(new Token.EndTag("select"));
                        return (tb.Process(t));
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return tb.Process(t, InSelect);
                }
            }
        };
        protected class AfterBodyState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (IsWhitespace(t))
                {
                    return tb.Process(t, InBody);
                }
                else if (t.IsComment())
                {
                    tb.Insert(t.AsComment()); // into html node
                }
                else if (t.IsDoctype())
                {
                    tb.Error(this);
                    return false;
                }
                else if (t.IsStartTag() && t.AsStartTag().Name().Equals("html"))
                {
                    return tb.Process(t, InBody);
                }
                else if (t.IsEndTag() && t.AsEndTag().Name().Equals("html"))
                {
                    if (tb.IsFragmentParsing())
                    {
                        tb.Error(this);
                        return false;
                    }
                    else
                    {
                        tb.Transition(AfterAfterBody);
                    }
                }
                else if (t.IsEOF())
                {
                    // chillax! we're done
                }
                else
                {
                    tb.Error(this);
                    tb.Transition(InBody);
                    return tb.Process(t);
                }
                return true;
            }
        };
        protected class InFramesetState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (IsWhitespace(t))
                {
                    tb.Insert(t.AsCharacter());
                }
                else if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                }
                else if (t.IsDoctype())
                {
                    tb.Error(this);
                    return false;
                }
                else if (t.IsStartTag())
                {
                    Token.StartTag start = t.AsStartTag();
                    string name = start.Name();
                    if (name.Equals("html"))
                    {
                        return tb.Process(start, InBody);
                    }
                    else if (name.Equals("frameset"))
                    {
                        tb.Insert(start);
                    }
                    else if (name.Equals("frame"))
                    {
                        tb.InsertEmpty(start);
                    }
                    else if (name.Equals("noframes"))
                    {
                        return tb.Process(start, InHead);
                    }
                    else
                    {
                        tb.Error(this);
                        return false;
                    }
                }
                else if (t.IsEndTag() && t.AsEndTag().Name().Equals("frameset"))
                {
                    if (tb.CurrentElement.NodeName.Equals("html"))
                    { // frag
                        tb.Error(this);
                        return false;
                    }
                    else
                    {
                        tb.Pop();
                        if (!tb.IsFragmentParsing() && !tb.CurrentElement.NodeName.Equals("frameset"))
                        {
                            tb.Transition(AfterFrameset);
                        }
                    }
                }
                else if (t.IsEOF())
                {
                    if (!tb.CurrentElement.NodeName.Equals("html"))
                    {
                        tb.Error(this);
                        return true;
                    }
                }
                else
                {
                    tb.Error(this);
                    return false;
                }
                return true;
            }
        };
        protected class AfterFramesetState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (IsWhitespace(t))
                {
                    tb.Insert(t.AsCharacter());
                }
                else if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                }
                else if (t.IsDoctype())
                {
                    tb.Error(this);
                    return false;
                }
                else if (t.IsStartTag() && t.AsStartTag().Name().Equals("html"))
                {
                    return tb.Process(t, InBody);
                }
                else if (t.IsEndTag() && t.AsEndTag().Name().Equals("html"))
                {
                    tb.Transition(AfterAfterFrameset);
                }
                else if (t.IsStartTag() && t.AsStartTag().Name().Equals("noframes"))
                {
                    return tb.Process(t, InHead);
                }
                else if (t.IsEOF())
                {
                    // cool your heels, we're complete
                }
                else
                {
                    tb.Error(this);
                    return false;
                }
                return true;
            }
        };
        protected class AfterAfterBodyState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                }
                else if (t.IsDoctype() || IsWhitespace(t) || (t.IsStartTag() && t.AsStartTag().Name().Equals("html")))
                {
                    return tb.Process(t, InBody);
                }
                else if (t.IsEOF())
                {
                    // nice work chuck
                }
                else
                {
                    tb.Error(this);
                    tb.Transition(InBody);
                    return tb.Process(t);
                }
                return true;
            }
        };
        protected class AfterAfterFramesetState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                if (t.IsComment())
                {
                    tb.Insert(t.AsComment());
                }
                else if (t.IsDoctype() || IsWhitespace(t) || (t.IsStartTag() && t.AsStartTag().Name().Equals("html")))
                {
                    return tb.Process(t, InBody);
                }
                else if (t.IsEOF())
                {
                    // nice work chuck
                }
                else if (t.IsStartTag() && t.AsStartTag().Name().Equals("noframes"))
                {
                    return tb.Process(t, InHead);
                }
                else
                {
                    tb.Error(this);
                    return false;
                }
                return true;
            }
        };
        protected class ForeignContentState : HtmlTreeBuilderState
        {
            public override bool Process(Token t, HtmlTreeBuilder tb)
            {
                return true;
                // todo: implement. Also; how do we get here?
            }
        };

        #endregion

        public static readonly HtmlTreeBuilderState Initial = new InitialState();
        public static readonly HtmlTreeBuilderState BeforeHtml = new BeforeHtmlState();
        public static readonly HtmlTreeBuilderState BeforeHead = new BeforeHeadState();
        public static readonly HtmlTreeBuilderState InHead = new InHeadState();
        public static readonly HtmlTreeBuilderState InHeadNoscript = new InHeadNoscriptState();
        public static readonly HtmlTreeBuilderState AfterHead = new AfterHeadState();
        public static readonly HtmlTreeBuilderState InBody = new InBodyState();
        public static readonly HtmlTreeBuilderState Text = new TextState();
        public static readonly HtmlTreeBuilderState InTable = new InTableState();
        public static readonly HtmlTreeBuilderState InTableText = new InTableTextState();
        public static readonly HtmlTreeBuilderState InCaption = new InCaptionState();
        public static readonly HtmlTreeBuilderState InColumnGroup = new InColumnGroupState();
        public static readonly HtmlTreeBuilderState InTableBody = new InTableBodyState();
        public static readonly HtmlTreeBuilderState InRow = new InRowState();
        public static readonly HtmlTreeBuilderState InCell = new InCellState();
        public static readonly HtmlTreeBuilderState InSelect = new InSelectState();
        public static readonly HtmlTreeBuilderState InSelectInTable = new InSelectInTableState();
        public static readonly HtmlTreeBuilderState AfterBody = new AfterBodyState();
        public static readonly HtmlTreeBuilderState InFrameset = new InFramesetState();
        public static readonly HtmlTreeBuilderState AfterFrameset = new AfterFramesetState();
        public static readonly HtmlTreeBuilderState AfterAfterBody = new AfterAfterBodyState();
        public static readonly HtmlTreeBuilderState AfterAfterFrameset = new AfterAfterFramesetState();
        public static readonly HtmlTreeBuilderState ForeignContent = new ForeignContentState();

        protected static string _nullString = "\u0000";

        public abstract bool Process(Token t, HtmlTreeBuilder tb);

        protected bool IsWhitespace(Token t)
        {
            if (t.IsCharacter())
            {
                string data = t.AsCharacter().Data.ToString();
                // todo: this checks more than spec - "\t", "\n", "\f", "\r", " "
                for (int i = 0; i < data.Length; i++)
                {
                    char c = data[i];
                    if (!StringUtil.IsWhiteSpace(c))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        protected void HandleRcData(Token.StartTag startTag, HtmlTreeBuilder tb)
        {
            tb.Insert(startTag);
            tb.Tokeniser.Transition(TokeniserState.RcData);
            tb.MarkInsertionMode();
            tb.Transition(Text);
        }

        protected void HandleRawText(Token.StartTag startTag, HtmlTreeBuilder tb)
        {
            tb.Insert(startTag);
            tb.Tokeniser.Transition(TokeniserState.RawText);
            tb.MarkInsertionMode();
            tb.Transition(Text);
        }

        public override string ToString()
        {
            return this.GetType().Name.Replace("State", string.Empty);
        }
    }
}
