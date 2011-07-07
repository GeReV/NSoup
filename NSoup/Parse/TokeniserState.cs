using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Parse
{
    /**
     * States and Transition activations for the Tokeniser.
     */
    internal abstract class TokeniserState
    {
        #region Subclasses

        protected class DataState : TokeniserState
        {
            // in data state, gather characters until a character reference or tag is found
            public override void Read(Tokeniser t, CharacterReader r)
            {
                switch (r.Current())
                {
                    case '&':
                        t.AdvanceTransition(CharacterReferenceInData);
                        break;
                    case '<':
                        t.AdvanceTransition(TagOpen);
                        break;
                    case _nullChar:
                        t.Error(this); // NOT replacement character (oddly?)
                        t.Emit(r.Consume());
                        break;
                    case _eof:
                        t.Emit(new Token.EOF());
                        break;
                    default:
                        string data = r.ConsumeToAny('&', '<', _nullChar);
                        t.Emit(data);
                        break;
                }
            }
        };
        protected class CharacterReferenceInDataState : TokeniserState
        {
            // from & in data
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char? c = t.ConsumeCharacterReference(null, false);

                if (c == null)
                {
                    t.Emit('&');
                }
                else
                {
                    t.Emit(c.Value);
                }

                t.Transition(Data);
            }
        };
        protected class RcDataState : TokeniserState
        {
            /// handles data in title, textarea etc
            public override void Read(Tokeniser t, CharacterReader r)
            {
                switch (r.Current())
                {
                    case '&':
                        t.AdvanceTransition(CharacterReferenceInRcData);
                        break;
                    case '<':
                        t.AdvanceTransition(RcDataLessThanSign);
                        break;
                    case _nullChar:
                        t.Error(this);
                        r.Advance();
                        t.Emit(_replacementChar);
                        break;
                    case _eof:
                        t.Emit(new Token.EOF());
                        break;
                    default:
                        string data = r.ConsumeToAny('&', '<', _nullChar);
                        t.Emit(data);
                        break;
                }
            }
        };
        protected class CharacterReferenceInRcDataState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char? c = t.ConsumeCharacterReference(null, false);
                if (c == null)
                {
                    t.Emit('&');
                }
                else
                {
                    t.Emit(c.Value);
                }
                t.Transition(RcData);
            }
        };
        protected class RawTextState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                switch (r.Current())
                {
                    case '<':
                        t.AdvanceTransition(RawTextLessThanSign);
                        break;
                    case _nullChar:
                        t.Error(this);
                        r.Advance();
                        t.Emit(_replacementChar);
                        break;
                    case _eof:
                        t.Emit(new Token.EOF());
                        break;
                    default:
                        string data = r.ConsumeToAny('<', _nullChar);
                        t.Emit(data);
                        break;
                }
            }
        };
        protected class ScriptDataState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                switch (r.Current())
                {
                    case '<':
                        t.AdvanceTransition(ScriptDataLessThanSign);
                        break;
                    case _nullChar:
                        t.Error(this);
                        r.Advance();
                        t.Emit(_replacementChar);
                        break;
                    case _eof:
                        t.Emit(new Token.EOF());
                        break;
                    default:
                        string data = r.ConsumeToAny('<', _nullChar);
                        t.Emit(data);
                        break;
                }
            }
        };
        protected class PlainTextState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                switch (r.Current())
                {
                    case _nullChar:
                        t.Error(this);
                        r.Advance();
                        t.Emit(_replacementChar);
                        break;
                    case _eof:
                        t.Emit(new Token.EOF());
                        break;
                    default:
                        string data = r.ConsumeTo(_nullChar);
                        t.Emit(data);
                        break;
                }
            }
        };
        protected class TagOpenState : TokeniserState
        {
            // from < in data
            public override void Read(Tokeniser t, CharacterReader r)
            {
                switch (r.Current())
                {
                    case '!':
                        t.AdvanceTransition(MarkupDeclarationOpen);
                        break;
                    case '/':
                        t.AdvanceTransition(EndTagOpen);
                        break;
                    case '?':
                        t.AdvanceTransition(BogusComment);
                        break;
                    default:
                        if (r.MatchesLetter())
                        {
                            t.CreateTagPending(true);
                            t.Transition(TagName);
                        }
                        else
                        {
                            t.Error(this);
                            t.Emit('<'); // char that got us here
                            t.Transition(Data);
                        }
                        break;
                }
            }
        };
        protected class EndTagOpenState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.IsEmpty())
                {
                    t.EofError(this);
                    t.Emit("</");
                    t.Transition(Data);
                }
                else if (r.MatchesLetter())
                {
                    t.CreateTagPending(false);
                    t.Transition(TagName);
                }
                else if (r.Matches('>'))
                {
                    t.Error(this);
                    t.AdvanceTransition(Data);
                }
                else
                {
                    t.Error(this);
                    t.AdvanceTransition(BogusComment);
                }
            }
        };
        protected class TagNameState : TokeniserState
        {
            // from < or </ in data, will have start or end tag pending
            public override void Read(Tokeniser t, CharacterReader r)
            {
                // previous TagOpen state did NOT Consume, will have a letter char in current
                string tagName = r.ConsumeToAny('\t', '\n', '\f', ' ', '/', '>', _nullChar).ToLowerInvariant();
                t.TagPending.AppendTagName(tagName);

                switch (r.Consume())
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(BeforeAttributeName);
                        break;
                    case '/':
                        t.Transition(SelfClosingStartTag);
                        break;
                    case '>':
                        t.EmitTagPending();
                        t.Transition(Data);
                        break;
                    case _nullChar: // replacement
                        t.TagPending.AppendTagName(_replacementStr);
                        break;
                    case _eof: // should Emit pending tag?
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    // no default, as covered with above ConsumeToAny
                }
            }
        };
        protected class RcDataLessThanSignState : TokeniserState
        {
            // from < in rcdata
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.Matches('/'))
                {
                    t.CreateTempBuffer();
                    t.AdvanceTransition(RcDataEndTagOpen);
                }
                else if (r.MatchesLetter() && !r.ContainsIgnoreCase("</" + t.AppropriateEndTagName()))
                {
                    // diverge from spec: got a start tag, but there's no appropriate end tag (</title>), so rather than
                    // consuming to EOF; break out here
                    t.TagPending = new Token.EndTag(t.AppropriateEndTagName());
                    t.EmitTagPending();
                    r.Unconsume(); // undo "<"
                    t.Transition(Data);
                }
                else
                {
                    t.Emit("<");
                    t.Transition(RcData);
                }
            }
        };
        protected class RcDataEndTagOpenState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    t.CreateTagPending(false);
                    t.TagPending.AppendTagName(char.ToLowerInvariant(r.Current()));
                    t.DataBuffer.Append(char.ToLowerInvariant(r.Current()));
                    t.AdvanceTransition(RcDataEndTagName);
                }
                else
                {
                    t.Emit("</");
                    t.Transition(RcData);
                }
            }
        };
        protected class RcDataEndTagNameState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    string name = r.ConsumeLetterSequence();
                    t.TagPending.AppendTagName(name.ToLowerInvariant());
                    t.DataBuffer.Append(name);
                    return;
                }

                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        if (t.IsAppropriateEndTagToken())
                        {
                            t.Transition(BeforeAttributeName);
                        }
                        else
                        {
                            AnythingElse(t, r);
                        }
                        break;
                    case '/':
                        if (t.IsAppropriateEndTagToken())
                        {
                            t.Transition(SelfClosingStartTag);
                        }
                        else
                        {
                            AnythingElse(t, r);
                        }
                        break;
                    case '>':
                        if (t.IsAppropriateEndTagToken())
                        {
                            t.EmitTagPending();
                            t.Transition(Data);
                        }
                        else
                        {
                            AnythingElse(t, r);
                        }
                        break;
                    default:
                        AnythingElse(t, r);
                        break;
                }
            }

            private void AnythingElse(Tokeniser t, CharacterReader r)
            {
                t.Emit("</" + t.DataBuffer.ToString());
                t.Transition(RcData);
            }
        };
        protected class RawTextLessThanSignState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.Matches('/'))
                {
                    t.CreateTempBuffer();
                    t.AdvanceTransition(RawTextEndTagOpen);
                }
                else
                {
                    t.Emit('<');
                    t.Transition(RawText);
                }
            }
        };
        protected class RawTextEndTagOpenState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    t.CreateTagPending(false);
                    t.Transition(RawTextEndTagName);
                }
                else
                {
                    t.Emit("</");
                    t.Transition(RawText);
                }
            }
        };
        protected class RawTextEndTagNameState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    string name = r.ConsumeLetterSequence();
                    t.TagPending.AppendTagName(name.ToLowerInvariant());
                    t.DataBuffer.Append(name);
                    return;
                }

                if (t.IsAppropriateEndTagToken() && !r.IsEmpty())
                {
                    char c = r.Consume();
                    switch (c)
                    {
                        case '\t':
                        case '\n':
                        case '\f':
                        case ' ':
                            t.Transition(BeforeAttributeName);
                            break;
                        case '/':
                            t.Transition(SelfClosingStartTag);
                            break;
                        case '>':
                            t.EmitTagPending();
                            t.Transition(Data);
                            break;
                        default:
                            t.DataBuffer.Append(c);
                            AnythingElse(t, r);
                            break;
                    }
                }
                else
                {
                    AnythingElse(t, r);
                }
            }

            private void AnythingElse(Tokeniser t, CharacterReader r)
            {
                t.Emit("</" + t.DataBuffer.ToString());
                t.Transition(RawText);
            }
        };
        protected class ScriptDataLessThanSignState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                switch (r.Consume())
                {
                    case '/':
                        t.CreateTempBuffer();
                        t.Transition(ScriptDataEndTagOpen);
                        break;
                    case '!':
                        t.Emit("<!");
                        t.Transition(ScriptDataEscapeStart);
                        break;
                    default:
                        t.Emit("<");
                        r.Unconsume();
                        t.Transition(ScriptData);
                        break;
                }
            }
        };
        protected class ScriptDataEndTagOpenState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    t.CreateTagPending(false);
                    t.Transition(ScriptDataEndTagName);
                }
                else
                {
                    t.Emit("</");
                    t.Transition(ScriptData);
                }

            }
        };
        protected class ScriptDataEndTagNameState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    string name = r.ConsumeLetterSequence();
                    t.TagPending.AppendTagName(name.ToLowerInvariant());
                    t.DataBuffer.Append(name);
                    return;
                }

                if (t.IsAppropriateEndTagToken() && !r.IsEmpty())
                {
                    char c = r.Consume();
                    switch (c)
                    {
                        case '\t':
                        case '\n':
                        case '\f':
                        case ' ':
                            t.Transition(BeforeAttributeName);
                            break;
                        case '/':
                            t.Transition(SelfClosingStartTag);
                            break;
                        case '>':
                            t.EmitTagPending();
                            t.Transition(Data);
                            break;
                        default:
                            t.DataBuffer.Append(c);
                            AnythingElse(t, r);
                            break;
                    }
                }
                else
                {
                    AnythingElse(t, r);
                }
            }

            private void AnythingElse(Tokeniser t, CharacterReader r)
            {
                t.Emit("</" + t.DataBuffer.ToString());
                t.Transition(ScriptData);
            }
        };
        protected class ScriptDataEscapeStartState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.Matches('-'))
                {
                    t.Emit('-');
                    t.AdvanceTransition(ScriptDataEscapeStartDash);
                }
                else
                {
                    t.Transition(ScriptData);
                }
            }
        };
        protected class ScriptDataEscapeStartDashState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.Matches('-'))
                {
                    t.Emit('-');
                    t.AdvanceTransition(ScriptDataEscapedDashDash);
                }
                else
                {
                    t.Transition(ScriptData);
                }
            }
        };
        protected class ScriptDataEscapedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.IsEmpty())
                {
                    t.EofError(this);
                    t.Transition(Data);
                    return;
                }

                switch (r.Current())
                {
                    case '-':
                        t.Emit('-');
                        t.AdvanceTransition(ScriptDataEscapedDash);
                        break;
                    case '<':
                        t.AdvanceTransition(ScriptDataEscapedLessThanSign);
                        break;
                    case _nullChar:
                        t.Error(this);
                        r.Advance();
                        t.Emit(_replacementChar);
                        break;
                    default:
                        string data = r.ConsumeToAny('-', '<', _nullChar);
                        t.Emit(data);
                        break;
                }
            }
        };
        protected class ScriptDataEscapedDashState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.IsEmpty())
                {
                    t.EofError(this);
                    t.Transition(Data);
                    return;
                }

                char c = r.Consume();
                switch (c)
                {
                    case '-':
                        t.Emit(c);
                        t.Transition(ScriptDataEscapedDashDash);
                        break;
                    case '<':
                        t.Transition(ScriptDataEscapedLessThanSign);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.Emit(_replacementChar);
                        t.Transition(ScriptDataEscaped);
                        break;
                    default:
                        t.Emit(c);
                        t.Transition(ScriptDataEscaped);
                        break;
                }
            }
        };
        protected class ScriptDataEscapedDashDashState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.IsEmpty())
                {
                    t.EofError(this);
                    t.Transition(Data);
                    return;
                }

                char c = r.Consume();
                switch (c)
                {
                    case '-':
                        t.Emit(c);
                        break;
                    case '<':
                        t.Transition(ScriptDataEscapedLessThanSign);
                        break;
                    case '>':
                        t.Emit(c);
                        t.Transition(ScriptData);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.Emit(_replacementChar);
                        t.Transition(ScriptDataEscaped);
                        break;
                    default:
                        t.Emit(c);
                        t.Transition(ScriptDataEscaped);
                        break;
                }
            }
        };
        protected class ScriptDataEscapedLessthanSignState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    t.CreateTempBuffer();
                    t.DataBuffer.Append(char.ToLowerInvariant(r.Current()));
                    t.Emit("<" + r.Current());
                    t.AdvanceTransition(ScriptDataDoubleEscapeStart);
                }
                else if (r.Matches('/'))
                {
                    t.CreateTempBuffer();
                    t.AdvanceTransition(ScriptDataEscapedEndTagOpen);
                }
                else
                {
                    t.Emit('<');
                    t.Transition(ScriptDataEscaped);
                }
            }
        };
        protected class ScriptDataEscapedEndTagOpenState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    t.CreateTagPending(false);
                    t.TagPending.AppendTagName(char.ToLowerInvariant(r.Current()));
                    t.DataBuffer.Append(r.Current());
                    t.AdvanceTransition(ScriptDataEscapedEndTagName);
                }
                else
                {
                    t.Emit("</");
                    t.Transition(ScriptDataEscaped);
                }
            }
        };
        protected class ScriptDataEscapedEndTagNameState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    string name = r.ConsumeLetterSequence();
                    t.TagPending.AppendTagName(name.ToLowerInvariant());
                    t.DataBuffer.Append(name);
                    r.Advance();
                    return;
                }

                if (t.IsAppropriateEndTagToken() && !r.IsEmpty())
                {
                    char c = r.Consume();
                    switch (c)
                    {
                        case '\t':
                        case '\n':
                        case '\f':
                        case ' ':
                            t.Transition(BeforeAttributeName);
                            break;
                        case '/':
                            t.Transition(SelfClosingStartTag);
                            break;
                        case '>':
                            t.EmitTagPending();
                            t.Transition(Data);
                            break;
                        default:
                            t.DataBuffer.Append(c);
                            AnythingElse(t, r);
                            break;
                    }
                }
                else
                {
                    AnythingElse(t, r);
                }
            }

            private void AnythingElse(Tokeniser t, CharacterReader r)
            {
                t.Emit("</" + t.DataBuffer.ToString());
                t.Transition(ScriptDataEscaped);
            }
        };
        protected class ScriptDataDoubleEscapeStartState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    string name = r.ConsumeLetterSequence();
                    t.DataBuffer.Append(name.ToLowerInvariant());
                    t.Emit(name);
                    return;
                }

                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                    case '/':
                    case '>':
                        if (t.DataBuffer.ToString().Equals("script"))
                        {
                            t.Transition(ScriptDataDoubleEscaped);
                        }
                        else
                        {
                            t.Transition(ScriptDataEscaped);
                        }
                        t.Emit(c);
                        break;
                    default:
                        r.Unconsume();
                        t.Transition(ScriptDataEscaped);
                        break;
                }
            }
        };
        protected class ScriptDataDoubleEscapedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Current();
                switch (c)
                {
                    case '-':
                        t.Emit(c);
                        t.AdvanceTransition(ScriptDataDoubleEscapedDash);
                        break;
                    case '<':
                        t.Emit(c);
                        t.AdvanceTransition(ScriptDataDoubleEscapedLessthanSign);
                        break;
                    case _nullChar:
                        t.Error(this);
                        r.Advance();
                        t.Emit(_replacementChar);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    default:
                        string data = r.ConsumeToAny('-', '<', _nullChar);
                        t.Emit(data);
                        break;
                }
            }
        };
        protected class ScriptDataDoubleEscapedDashState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '-':
                        t.Emit(c);
                        t.Transition(ScriptDataDoubleEscapedDashDash);
                        break;
                    case '<':
                        t.Emit(c);
                        t.Transition(ScriptDataDoubleEscapedLessthanSign);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.Emit(_replacementChar);
                        t.Transition(ScriptDataDoubleEscaped);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    default:
                        t.Emit(c);
                        t.Transition(ScriptDataDoubleEscaped);
                        break;
                }
            }
        };
        protected class ScriptDataDoubleEscapedDashDashState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '-':
                        t.Emit(c);
                        break;
                    case '<':
                        t.Emit(c);
                        t.Transition(ScriptDataDoubleEscapedLessthanSign);
                        break;
                    case '>':
                        t.Emit(c);
                        t.Transition(ScriptData);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.Emit(_replacementChar);
                        t.Transition(ScriptDataDoubleEscaped);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    default:
                        t.Emit(c);
                        t.Transition(ScriptDataDoubleEscaped);
                        break;
                }
            }
        };
        protected class ScriptDataDoubleEscapedLessThanSignState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.Matches('/'))
                {
                    t.Emit('/');
                    t.CreateTempBuffer();
                    t.AdvanceTransition(ScriptDataDoubleEscapeEnd);
                }
                else
                {
                    t.Transition(ScriptDataDoubleEscaped);
                }
            }
        };
        protected class ScriptDataDoubleEscapeEndState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    string name = r.ConsumeLetterSequence();
                    t.DataBuffer.Append(name.ToLowerInvariant());
                    t.Emit(name);
                    return;
                }

                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                    case '/':
                    case '>':
                        if (t.DataBuffer.ToString().Equals("script"))
                        {
                            t.Transition(ScriptDataEscaped);
                        }
                        else
                        {
                            t.Transition(ScriptDataDoubleEscaped);
                        }
                        t.Emit(c);
                        break;
                    default:
                        r.Unconsume();
                        t.Transition(ScriptDataDoubleEscaped);
                        break;
                }
            }
        };
        protected class BeforeAttributeNameState : TokeniserState
        {
            // from tagname <xxx
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        break; // ignore whitespace
                    case '/':
                        t.Transition(SelfClosingStartTag);
                        break;
                    case '>':
                        t.EmitTagPending();
                        t.Transition(Data);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.TagPending.NewAttribute();
                        r.Unconsume();
                        t.Transition(AttributeName);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    case '"':
                    case '\'':
                    case '<':
                    case '=':
                        t.Error(this);
                        t.TagPending.NewAttribute();
                        t.TagPending.AppendAttributeName(c);
                        t.Transition(AttributeName);
                        break;
                    default: // A-Z, anything else
                        t.TagPending.NewAttribute();
                        r.Unconsume();
                        t.Transition(AttributeName);
                        break;
                }
            }
        };
        protected class AttributeNameState : TokeniserState
        {
            // from before attribute name
            public override void Read(Tokeniser t, CharacterReader r)
            {
                string name = r.ConsumeToAny('\t', '\n', '\f', ' ', '/', '=', '>', _nullChar, '"', '\'', '<');
                t.TagPending.AppendAttributeName(name.ToLowerInvariant());

                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(AfterAttributeName);
                        break;
                    case '/':
                        t.Transition(SelfClosingStartTag);
                        break;
                    case '=':
                        t.Transition(BeforeAttributeValue);
                        break;
                    case '>':
                        t.EmitTagPending();
                        t.Transition(Data);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.TagPending.AppendAttributeName(_replacementChar);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    case '"':
                    case '\'':
                    case '<':
                        t.Error(this);
                        t.TagPending.AppendAttributeName(c);
                        break;
                    // no default, as covered in ConsumeToAny
                }
            }
        };
        protected class AfterAttributeNameState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        // ignore
                        break;
                    case '/':
                        t.Transition(SelfClosingStartTag);
                        break;
                    case '=':
                        t.Transition(BeforeAttributeValue);
                        break;
                    case '>':
                        t.EmitTagPending();
                        t.Transition(Data);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.TagPending.AppendAttributeName(_replacementChar);
                        t.Transition(AttributeName);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    case '"':
                    case '\'':
                    case '<':
                        t.Error(this);
                        t.TagPending.NewAttribute();
                        t.TagPending.AppendAttributeName(c);
                        t.Transition(AttributeName);
                        break;
                    default: // A-Z, anything else
                        t.TagPending.NewAttribute();
                        r.Unconsume();
                        t.Transition(AttributeName);
                        break;
                }
            }
        };
        protected class BeforeAttributeValueState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        // ignore
                        break;
                    case '"':
                        t.Transition(AttributeValueDoubleQuoted);
                        break;
                    case '&':
                        r.Unconsume();
                        t.Transition(AttributeValueUnquoted);
                        break;
                    case '\'':
                        t.Transition(AttributeValueSingleQuoted);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.TagPending.AppendAttributeValue(_replacementChar);
                        t.Transition(AttributeValueUnquoted);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    case '>':
                        t.Error(this);
                        t.EmitTagPending();
                        t.Transition(Data);
                        break;
                    case '<':
                    case '=':
                    case '`':
                        t.Error(this);
                        t.TagPending.AppendAttributeValue(c);
                        t.Transition(AttributeValueUnquoted);
                        break;
                    default:
                        r.Unconsume();
                        t.Transition(AttributeValueUnquoted);
                        break;
                }
            }
        };
        protected class AttributeValueDoubleQuotedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                string value = r.ConsumeToAny('"', '&', _nullChar);
                if (value.Length > 0)
                {
                    t.TagPending.AppendAttributeValue(value);
                }

                char c = r.Consume();
                switch (c)
                {
                    case '"':
                        t.Transition(AfterAttributeValueQuoted);
                        break;
                    case '&':
                        char? reference = t.ConsumeCharacterReference('"', true);
                        if (reference != null)
                        {
                            t.TagPending.AppendAttributeValue(reference.Value);
                        }
                        else
                        {
                            t.TagPending.AppendAttributeValue('&');
                        }
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.TagPending.AppendAttributeValue(_replacementChar);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    // no default, handled in Consume to any above
                }
            }
        };
        protected class AttributeValueSingleQuotedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                string value = r.ConsumeToAny('\'', '&', _nullChar);
                if (value.Length > 0)
                {
                    t.TagPending.AppendAttributeValue(value);
                }

                char c = r.Consume();
                switch (c)
                {
                    case '\'':
                        t.Transition(AfterAttributeValueQuoted);
                        break;
                    case '&':
                        char? reference = t.ConsumeCharacterReference('\'', true);
                        if (reference != null)
                        {
                            t.TagPending.AppendAttributeValue(reference.Value);
                        }
                        else
                        {
                            t.TagPending.AppendAttributeValue('&');
                        }
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.TagPending.AppendAttributeValue(_replacementChar);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    // no default, handled in Consume to any above
                }
            }
        };
        protected class AttributeValueUnquotedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                string value = r.ConsumeToAny('\t', '\n', '\f', ' ', '&', '>', _nullChar, '"', '\'', '<', '=', '`');
                if (value.Length > 0)
                {
                    t.TagPending.AppendAttributeValue(value);
                }

                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(BeforeAttributeName);
                        break;
                    case '&':
                        char? reference = t.ConsumeCharacterReference('>', true);
                        if (reference != null)
                        {
                            t.TagPending.AppendAttributeValue(reference.Value);
                        }
                        else
                        {
                            t.TagPending.AppendAttributeValue('&');
                        }
                        break;
                    case '>':
                        t.EmitTagPending();
                        t.Transition(Data);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.TagPending.AppendAttributeValue(_replacementChar);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    case '"':
                    case '\'':
                    case '<':
                    case '=':
                    case '`':
                        t.Error(this);
                        t.TagPending.AppendAttributeValue(c);
                        break;
                    // no default, handled in Consume to any above
                }

            }
        };
        // CharacterReferenceInAttributeValue state handled inline
        protected class AfterAttributeValueQuotedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(BeforeAttributeName);
                        break;
                    case '/':
                        t.Transition(SelfClosingStartTag);
                        break;
                    case '>':
                        t.EmitTagPending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        r.Unconsume();
                        t.Transition(BeforeAttributeName);
                        break;
                }
            }
        };
        protected class SelfClosingStartTagState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '>':
                        t.TagPending.IsSelfClosing = true;
                        t.EmitTagPending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.Transition(BeforeAttributeName);
                        break;
                }
            }
        };
        protected class BogusCommentState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                // todo: handle bogus comment starting from eof. when does that trigger?
                // rewind to capture character that lead us here
                r.Unconsume();
                Token.Comment comment = new Token.Comment();
                comment.Data.Append(r.ConsumeTo('>'));
                // todo: replace nullChar with replaceChar
                t.Emit(comment);
                t.AdvanceTransition(Data);
            }
        };
        protected class MarkupDeclarationOpenState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchConsume("--"))
                {
                    t.CreateCommentPending();
                    t.Transition(CommentStart);
                }
                else if (r.MatchConsumeIgnoreCase("DOCTYPE"))
                {
                    t.Transition(Doctype);
                }
                else if (r.MatchConsume("[CDATA["))
                {
                    // todo: should actually check current namepspace, and only non-html allows cdata. until namespace
                    // is implemented properly, keep handling as cdata
                    //} else if (!t.currentNodeInHtmlNS() && r.matchConsume("[CDATA[")) {
                    t.Transition(CDataSection);
                }
                else
                {
                    t.Error(this);
                    t.AdvanceTransition(BogusComment); // advance so this character gets in bogus comment data's rewind
                }
            }
        };
        protected class CommentStartState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '-':
                        t.Transition(CommentStartDash);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.CommentPending.Data.Append(_replacementChar);
                        t.Transition(Comment);
                        break;
                    case '>':
                        t.Error(this);
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    default:
                        t.CommentPending.Data.Append(c);
                        t.Transition(Comment);
                        break;
                }
            }
        };
        protected class CommentStartDashState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '-':
                        t.Transition(CommentStartDash);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.CommentPending.Data.Append(_replacementChar);
                        t.Transition(Comment);
                        break;
                    case '>':
                        t.Error(this);
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    default:
                        t.CommentPending.Data.Append(c);
                        t.Transition(Comment);
                        break;
                }
            }
        };
        protected class CommentState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Current();
                switch (c)
                {
                    case '-':
                        t.AdvanceTransition(CommentEndDash);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.CommentPending.Data.Append(_replacementChar);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    default:
                        t.CommentPending.Data.Append(r.ConsumeToAny('-', _nullChar));
                        break;
                }
            }
        };
        protected class CommentEndDashState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '-':
                        t.Transition(CommentEnd);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.CommentPending.Data.Append('-').Append(_replacementChar);
                        t.Transition(Comment);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    default:
                        t.CommentPending.Data.Append('-').Append(c);
                        t.Transition(Comment);
                        break;
                }
            }
        };
        protected class CommentEndState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '>':
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.CommentPending.Data.Append("--").Append(_replacementChar);
                        t.Transition(Comment);
                        break;
                    case '!':
                        t.Error(this);
                        t.Transition(CommentEndBang);
                        break;
                    case '-':
                        t.Error(this);
                        t.CommentPending.Data.Append('-');
                        break;
                    case _eof:
                        t.EofError(this);
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.CommentPending.Data.Append("--").Append(c);
                        t.Transition(Comment);
                        break;
                }
            }
        };
        protected class CommentEndBangState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '-':
                        t.CommentPending.Data.Append("--!");
                        t.Transition(CommentEndDash);
                        break;
                    case '>':
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.CommentPending.Data.Append("--!").Append(_replacementChar);
                        t.Transition(Comment);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.EmitCommentPending();
                        t.Transition(Data);
                        break;
                    default:
                        t.CommentPending.Data.Append("--!").Append(c);
                        t.Transition(Comment);
                        break;
                }
            }
        };
        protected class DoctypeState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(BeforeDoctypeName);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.CreateDoctypePending();
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.Transition(BeforeDoctypeName);
                        break;
                }
            }
        };
        protected class BeforeDoctypeNameState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    t.CreateDoctypePending();
                    t.Transition(DoctypeName);
                    return;
                }
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        break; // ignore whitespace
                    case _nullChar:
                        t.Error(this);
                        t.DoctypePending.Name.Append(_replacementChar);
                        t.Transition(DoctypeName);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.CreateDoctypePending();
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.CreateDoctypePending();
                        t.DoctypePending.Name.Append(c);
                        t.Transition(DoctypeName);
                        break;
                }
            }
        };
        protected class DoctypeNameState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.MatchesLetter())
                {
                    string name = r.ConsumeLetterSequence();
                    t.DoctypePending.Name.Append(name.ToLowerInvariant());
                    return;
                }
                char c = r.Consume();
                switch (c)
                {
                    case '>':
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(AfterDoctypeName);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.DoctypePending.Name.Append(_replacementChar);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.DoctypePending.Name.Append(c);
                        break;
                }
            }
        };
        protected class AfterDoctypeNameState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                if (r.IsEmpty())
                {
                    t.EofError(this);
                    t.DoctypePending.ForceQuirks = true;
                    t.EmitDoctypePending();
                    t.Transition(Data);
                    return;
                }
                if (r.Matches('>'))
                {
                    t.EmitDoctypePending();
                    t.AdvanceTransition(Data);
                }
                else if (r.MatchConsumeIgnoreCase("PUBLIC"))
                {
                    t.Transition(AfterDoctypePublicKeyword);
                }
                else if (r.MatchConsumeIgnoreCase("SYSTEM"))
                {
                    t.Transition(AfterDoctypeSystemKeyword);
                }
                else
                {
                    t.Error(this);
                    t.DoctypePending.ForceQuirks = true;
                    t.AdvanceTransition(BogusDoctype);
                }

            }
        };
        protected class AfterDoctypePublicKeywordState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(BeforeDoctypePublicIdentifier);
                        break;
                    case '"':
                        t.Error(this);
                        // set public id to empty string
                        t.Transition(DoctypePublicIdentifierDoubleQuoted);
                        break;
                    case '\'':
                        t.Error(this);
                        // set public id to empty string
                        t.Transition(DoctypePublicIdentifierSingleQuoted);
                        break;
                    case '>':
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.Transition(BogusDoctype);
                        break;
                }
            }
        };
        protected class BeforeDoctypePublicIdentifierState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        break;
                    case '"':
                        // set public id to empty string
                        t.Transition(DoctypePublicIdentifierDoubleQuoted);
                        break;
                    case '\'':
                        // set public id to empty string
                        t.Transition(DoctypePublicIdentifierSingleQuoted);
                        break;
                    case '>':
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.Transition(BogusDoctype);
                        break;
                }
            }
        };
        protected class DoctypePublicIdentifierDoubleQuotedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '"':
                        t.Transition(AfterDoctypePublicIdentifier);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.DoctypePending.PublicIdentifier.Append(_replacementChar);
                        break;
                    case '>':
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.DoctypePending.PublicIdentifier.Append(c);
                        break;
                }
            }
        };
        protected class DoctypePublicIdentifierSingleQuotedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\'':
                        t.Transition(AfterDoctypePublicIdentifier);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.DoctypePending.PublicIdentifier.Append(_replacementChar);
                        break;
                    case '>':
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.DoctypePending.PublicIdentifier.Append(c);
                        break;
                }
            }
        };
        protected class AfterDoctypePublicIdentifierState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(BetweenDoctypePublicAndSystemIdentifiers);
                        break;
                    case '>':
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case '"':
                        t.Error(this);
                        // system id empty
                        t.Transition(DoctypeSystemIdentifierDoubleQuoted);
                        break;
                    case '\'':
                        t.Error(this);
                        // system id empty
                        t.Transition(DoctypeSystemIdentifierSingleQuoted);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.Transition(BogusDoctype);
                        break;
                }
            }
        };
        protected class BetweenDoctypePublicAndSystemIdentifiersState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        break;
                    case '>':
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case '"':
                        t.Error(this);
                        // system id empty
                        t.Transition(DoctypeSystemIdentifierDoubleQuoted);
                        break;
                    case '\'':
                        t.Error(this);
                        // system id empty
                        t.Transition(DoctypeSystemIdentifierSingleQuoted);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.Transition(BogusDoctype);
                        break;
                }
            }
        };
        protected class AfterDoctypeSystemKeywordState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        t.Transition(BeforeDoctypeSystemIdentifier);
                        break;
                    case '>':
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case '"':
                        t.Error(this);
                        // system id empty
                        t.Transition(DoctypeSystemIdentifierDoubleQuoted);
                        break;
                    case '\'':
                        t.Error(this);
                        // system id empty
                        t.Transition(DoctypeSystemIdentifierSingleQuoted);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        break;
                }
            }
        };
        protected class BeforeDoctypeSystemIdentifierState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        break;
                    case '"':
                        // set system id to empty string
                        t.Transition(DoctypeSystemIdentifierDoubleQuoted);
                        break;
                    case '\'':
                        // set public id to empty string
                        t.Transition(DoctypeSystemIdentifierSingleQuoted);
                        break;
                    case '>':
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.Transition(BogusDoctype);
                        break;
                }
            }
        };
        protected class DoctypeSystemIdentifierDoubleQuotedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '"':
                        t.Transition(AfterDoctypeSystemIdentifier);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.DoctypePending.SystemIdentifier.Append(_replacementChar);
                        break;
                    case '>':
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.DoctypePending.SystemIdentifier.Append(c);
                        break;
                }
            }
        };
        protected class DoctypeSystemIdentifierSingleQuotedState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\'':
                        t.Transition(AfterDoctypeSystemIdentifier);
                        break;
                    case _nullChar:
                        t.Error(this);
                        t.DoctypePending.SystemIdentifier.Append(_replacementChar);
                        break;
                    case '>':
                        t.Error(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.DoctypePending.SystemIdentifier.Append(c);
                        break;
                }
            }
        };
        protected class AfterDoctypeSystemIdentifierState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '\t':
                    case '\n':
                    case '\f':
                    case ' ':
                        break;
                    case '>':
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EofError(this);
                        t.DoctypePending.ForceQuirks = true;
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        t.Error(this);
                        t.Transition(BogusDoctype);
                        break;
                    // NOT force quirks
                }
            }
        };
        protected class BogusDoctypeState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                char c = r.Consume();
                switch (c)
                {
                    case '>':
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    case _eof:
                        t.EmitDoctypePending();
                        t.Transition(Data);
                        break;
                    default:
                        // ignore char
                        break;
                }
            }
        };
        protected class CDataSectionState : TokeniserState
        {
            public override void Read(Tokeniser t, CharacterReader r)
            {
                string data = r.ConsumeTo("]]>");
                t.Emit(data);
                r.MatchConsume("]]>");
                t.Transition(Data);
            }
        };

        #endregion

        public abstract void Read(Tokeniser t, CharacterReader r);

        public static readonly TokeniserState Data = new DataState();
        public static readonly TokeniserState CharacterReferenceInData = new CharacterReferenceInDataState();
        public static readonly TokeniserState RcData = new RcDataState();
        public static readonly TokeniserState CharacterReferenceInRcData = new CharacterReferenceInRcDataState();
        public static readonly TokeniserState RawText = new RawTextState();
        public static readonly TokeniserState ScriptData = new ScriptDataState();
        public static readonly TokeniserState PlainText = new PlainTextState();
        public static readonly TokeniserState TagOpen = new TagOpenState();
        public static readonly TokeniserState EndTagOpen = new EndTagOpenState();
        public static readonly TokeniserState TagName = new TagNameState();
        public static readonly TokeniserState RcDataLessThanSign = new RcDataLessThanSignState();
        public static readonly TokeniserState RcDataEndTagOpen = new RcDataEndTagOpenState();
        public static readonly TokeniserState RcDataEndTagName = new RcDataEndTagNameState();
        public static readonly TokeniserState RawTextLessThanSign = new RawTextLessThanSignState();
        public static readonly TokeniserState RawTextEndTagOpen = new RawTextEndTagOpenState();
        public static readonly TokeniserState RawTextEndTagName = new RawTextEndTagNameState();
        public static readonly TokeniserState ScriptDataLessThanSign = new ScriptDataLessThanSignState();
        public static readonly TokeniserState ScriptDataEndTagOpen = new ScriptDataEndTagOpenState();
        public static readonly TokeniserState ScriptDataEndTagName = new ScriptDataEndTagNameState();
        public static readonly TokeniserState ScriptDataEscapeStart = new ScriptDataEscapeStartState();
        public static readonly TokeniserState ScriptDataEscapeStartDash = new ScriptDataEscapeStartDashState();
        public static readonly TokeniserState ScriptDataEscaped = new ScriptDataEscapedState();
        public static readonly TokeniserState ScriptDataEscapedDash = new ScriptDataEscapedDashState();
        public static readonly TokeniserState ScriptDataEscapedDashDash = new ScriptDataEscapedDashDashState();
        public static readonly TokeniserState ScriptDataEscapedLessThanSign = new ScriptDataEscapedLessthanSignState();
        public static readonly TokeniserState ScriptDataEscapedEndTagOpen = new ScriptDataEscapedEndTagOpenState();
        public static readonly TokeniserState ScriptDataEscapedEndTagName = new ScriptDataEscapedEndTagNameState();
        public static readonly TokeniserState ScriptDataDoubleEscapeStart = new ScriptDataDoubleEscapeStartState();
        public static readonly TokeniserState ScriptDataDoubleEscaped = new ScriptDataDoubleEscapedState();
        public static readonly TokeniserState ScriptDataDoubleEscapedDash = new ScriptDataDoubleEscapedDashState();
        public static readonly TokeniserState ScriptDataDoubleEscapedDashDash = new ScriptDataDoubleEscapedDashDashState();
        public static readonly TokeniserState ScriptDataDoubleEscapedLessthanSign = new ScriptDataDoubleEscapedLessThanSignState();
        public static readonly TokeniserState ScriptDataDoubleEscapeEnd = new ScriptDataDoubleEscapeEndState();
        public static readonly TokeniserState BeforeAttributeName = new BeforeAttributeNameState();
        public static readonly TokeniserState AttributeName = new AttributeNameState();
        public static readonly TokeniserState AfterAttributeName = new AfterAttributeNameState();
        public static readonly TokeniserState BeforeAttributeValue = new BeforeAttributeValueState();
        public static readonly TokeniserState AttributeValueDoubleQuoted = new AttributeValueDoubleQuotedState();
        public static readonly TokeniserState AttributeValueSingleQuoted = new AttributeValueSingleQuotedState();
        public static readonly TokeniserState AttributeValueUnquoted = new AttributeValueUnquotedState();
        public static readonly TokeniserState AfterAttributeValueQuoted = new AfterAttributeValueQuotedState();
        public static readonly TokeniserState SelfClosingStartTag = new SelfClosingStartTagState();
        public static readonly TokeniserState BogusComment = new BogusCommentState();
        public static readonly TokeniserState MarkupDeclarationOpen = new MarkupDeclarationOpenState();
        public static readonly TokeniserState CommentStart = new CommentStartState();
        public static readonly TokeniserState CommentStartDash = new CommentStartDashState();
        public static readonly TokeniserState Comment = new CommentState();
        public static readonly TokeniserState CommentEndDash = new CommentEndDashState();
        public static readonly TokeniserState CommentEnd = new CommentEndState();
        public static readonly TokeniserState CommentEndBang = new CommentEndBangState();
        public static readonly TokeniserState Doctype = new DoctypeState();
        public static readonly TokeniserState BeforeDoctypeName = new BeforeDoctypeNameState();
        public static readonly TokeniserState DoctypeName = new DoctypeNameState();
        public static readonly TokeniserState AfterDoctypeName = new AfterDoctypeNameState();
        public static readonly TokeniserState AfterDoctypePublicKeyword = new AfterDoctypePublicKeywordState();
        public static readonly TokeniserState BeforeDoctypePublicIdentifier = new BeforeDoctypePublicIdentifierState();
        public static readonly TokeniserState DoctypePublicIdentifierDoubleQuoted = new DoctypePublicIdentifierDoubleQuotedState();
        public static readonly TokeniserState DoctypePublicIdentifierSingleQuoted = new DoctypePublicIdentifierSingleQuotedState();
        public static readonly TokeniserState AfterDoctypePublicIdentifier = new AfterDoctypePublicIdentifierState();
        public static readonly TokeniserState BetweenDoctypePublicAndSystemIdentifiers = new BetweenDoctypePublicAndSystemIdentifiersState();
        public static readonly TokeniserState AfterDoctypeSystemKeyword = new AfterDoctypeSystemKeywordState();
        public static readonly TokeniserState BeforeDoctypeSystemIdentifier = new BeforeDoctypeSystemIdentifierState();
        public static readonly TokeniserState DoctypeSystemIdentifierDoubleQuoted = new DoctypeSystemIdentifierDoubleQuotedState();
        public static readonly TokeniserState DoctypeSystemIdentifierSingleQuoted = new DoctypeSystemIdentifierSingleQuotedState();
        public static readonly TokeniserState AfterDoctypeSystemIdentifier = new AfterDoctypeSystemIdentifierState();
        public static readonly TokeniserState BogusDoctype = new BogusDoctypeState();
        public static readonly TokeniserState CDataSection = new CDataSectionState();

        private const char _nullChar = '\u0000';
        private static readonly char _replacementChar = Tokeniser.ReplacementChar;
        private static readonly string _replacementStr = new string(Tokeniser.ReplacementChar, 1);
        private const char _eof = CharacterReader.EOF;
    }
}