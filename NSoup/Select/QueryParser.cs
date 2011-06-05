using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Parse;
using System.Text.RegularExpressions;

namespace NSoup.Select
{
    /// <summary>
    /// Parses a CSS selector into an Evaluator tree.
    /// </summary>
class QueryParser {
    private readonly static string[] combinators = {",", ">", "+", "~", " "};

    private TokenQueue _tq;
    private string _query;
    private List<Evaluator> _evals = new List<Evaluator>();

    /// <summary>
    /// Create a new QueryParser.
    /// </summary>
    /// <param name="query">query CSS query</param>
    private QueryParser(string query)
    {
        this._query = query;
        this._tq = new TokenQueue(query);
    }

    /// <summary>
    /// Parse a CSS query into an Evaluator.
    /// </summary>
    /// <param name="query">query CSS query</param>
    /// <returns>Evaluator</returns>
    public static Evaluator Parse(string query)
    {
        QueryParser p = new QueryParser(query);
        return p.Parse();
    }

    /// <summary>
    /// Parse the query
    /// </summary>
    /// <returns>Evaluator</returns>
    private Evaluator Parse() {
        _tq.ConsumeWhitespace();

        if (_tq.MatchesAny(combinators)) { // if starts with a combinator, use root as elements
            _evals.Add(new StructuralEvaluator.Root());
            Combinator(_tq.Consume());
        } else {
            FindElements();
        }

        while (!_tq.IsEmpty) {
            // hierarchy and extras
            bool seenWhite = _tq.ConsumeWhitespace();

            if (_tq.MatchChomp(",")) { // group or
                CombiningEvaluator.Or or = new CombiningEvaluator.Or(_evals);
                
                _evals.Clear();
                _evals.Add(or);

                while (!_tq.IsEmpty)
                {
                    string subQuery = _tq.ChompTo(",");
                    or.Add(Parse(subQuery));
                }
            } else if (_tq.MatchesAny(combinators)) {
                Combinator(_tq.Consume());
            } else if (seenWhite) {
                Combinator(' ');
            } else { // E.class, E#id, E[attr] etc. AND
                FindElements(); // take next el, #. etc off queue
            }
        }

        if (_evals.Count == 1)
        {
            return _evals[0];
        }

        return new CombiningEvaluator.And(_evals);
    }

    private void Combinator(char combinator) {
        _tq.ConsumeWhitespace();

        string subQuery = ConsumeSubQuery(); // support multi > childs
        
        Evaluator e;

        if (_evals.Count == 1)
        {
            e = _evals[0];
        }
        else
        {
            e = new CombiningEvaluator.And(_evals);
        }
        
        _evals.Clear();

        Evaluator f = Parse(subQuery);

        if (combinator == '>')
        {
            _evals.Add(new CombiningEvaluator.And(f, new StructuralEvaluator.ImmediateParent(e)));
        }
        else if (combinator == ' ')
        {
            _evals.Add(new CombiningEvaluator.And(f, new StructuralEvaluator.Parent(e)));
        }
        else if (combinator == '+')
        {
            _evals.Add(new CombiningEvaluator.And(f, new StructuralEvaluator.ImmediatePreviousSibling(e)));
        }
        else if (combinator == '~')
        {
            _evals.Add(new CombiningEvaluator.And(f, new StructuralEvaluator.PreviousSibling(e)));
        }
        else
        {
            throw new Selector.SelectorParseException("Unknown combinator: " + combinator);
        }
    }

    private string ConsumeSubQuery()
    {
        StringBuilder sq = new StringBuilder();
        while (!_tq.IsEmpty)
        {
            if (_tq.Matches("("))
            {
                sq.Append("(").Append(_tq.ChompBalanced('(', ')')).Append(")");
            }
            else if (_tq.Matches("["))
            {
                sq.Append("[").Append(_tq.ChompBalanced('[', ']')).Append("]");
            }
            else if (_tq.MatchesAny(combinators))
            {
                break;
            }
            else
            {
                sq.Append(_tq.Consume());
            }
        }
        return sq.ToString();
    }

    private void FindElements() {
        if (_tq.MatchChomp("#"))
        {
            ById();
        }
        else if (_tq.MatchChomp("."))
        {
            ByClass();
        }
        else if (_tq.MatchesWord())
        {
            ByTag();
        }
        else if (_tq.Matches("["))
        {
            ByAttribute();
        }
        else if (_tq.MatchChomp("*"))
        {
            AllElements();
        }
        else if (_tq.MatchChomp(":lt("))
        {
            IndexLessThan();
        }
        else if (_tq.MatchChomp(":gt("))
        {
            IndexGreaterThan();
        }
        else if (_tq.MatchChomp(":eq("))
        {
            IndexEquals();
        }
        else if (_tq.Matches(":has("))
        {
            Has();
        }
        else if (_tq.Matches(":contains("))
        {
            Contains(false);
        }
        else if (_tq.Matches(":containsOwn("))
        {
            Contains(true);
        }
        else if (_tq.Matches(":matches("))
        {
            Matches(false);
        }
        else if (_tq.Matches(":matchesOwn("))
        {
            Matches(true);
        }
        else if (_tq.Matches(":not("))
        {
            Not();
        }
        else // unhandled
        {
            throw new Selector.SelectorParseException("Could not parse query '{0}': unexpected token at '{1}'", _query, _tq.Remainder());
        }
    }

    private void ById()
    {
        string id = _tq.ConsumeCssIdentifier();

        if (string.IsNullOrEmpty(id))
        {
            throw new Exception("id is empty.");
        }

        _evals.Add(new Evaluator.Id(id));
    }

    private void ByClass() {
        string className = _tq.ConsumeCssIdentifier();

        if (string.IsNullOrEmpty(className))
        {
            throw new Exception("className is empty.");
        }
        
        _evals.Add(new Evaluator.Class(className.Trim().ToLowerInvariant()));
    }

    private void ByTag() {
        string tagName = _tq.ConsumeElementSelector();

        if (string.IsNullOrEmpty(tagName))
        {
            throw new Exception("tagName is empty.");
        }

        // namespaces: if element name is "abc:def", selector must be "abc|def", so flip:
        if (tagName.Contains("|"))
        {
            tagName = tagName.Replace("|", ":");
        }

        _evals.Add(new Evaluator.Tag(tagName.Trim().ToLowerInvariant()));
    }

    private void ByAttribute()
    {
        TokenQueue cq = new TokenQueue(_tq.ChompBalanced('[', ']')); // content queue
        string key = cq.ConsumeToAny("=", "!=", "^=", "$=", "*=", "~="); // eq, not, start, end, contain, match, (no val)

        if (string.IsNullOrEmpty(key))
        {
            throw new Exception("key is empty.");
        }

        cq.ConsumeWhitespace();

        if (cq.IsEmpty)
        {
            if (key.StartsWith("^"))
            {
                _evals.Add(new Evaluator.AttributeStarting(key.Substring(1)));
            }
            else
            {
                _evals.Add(new Evaluator.Attribute(key));
            }
        }
        else
        {
            if (cq.MatchChomp("="))
            {
                _evals.Add(new Evaluator.AttributeWithValue(key, cq.Remainder()));
            }
            else if (cq.MatchChomp("!="))
            {
                _evals.Add(new Evaluator.AttributeWithValueNot(key, cq.Remainder()));
            }
            else if (cq.MatchChomp("^="))
            {
                _evals.Add(new Evaluator.AttributeWithValueStarting(key, cq.Remainder()));
            }
            else if (cq.MatchChomp("$="))
            {
                _evals.Add(new Evaluator.AttributeWithValueEnding(key, cq.Remainder()));
            }
            else if (cq.MatchChomp("*="))
            {
                _evals.Add(new Evaluator.AttributeWithValueContaining(key, cq.Remainder()));
            }
            else if (cq.MatchChomp("~="))
            {
                _evals.Add(new Evaluator.AttributeWithValueMatching(key, new Regex(cq.Remainder())));
            }
            else
            {
                throw new Selector.SelectorParseException("Could not parse attribute query '{0}': unexpected token at '{1}'", _query, cq.Remainder());
            }
        }
    }

    private void AllElements()
    {
        _evals.Add(new Evaluator.AllElements());
    }

    // pseudo selectors :lt, :gt, :eq
    private void IndexLessThan() {
        _evals.Add(new Evaluator.IndexLessThan(ConsumeIndex()));
    }

    private void IndexGreaterThan() {
        _evals.Add(new Evaluator.IndexGreaterThan(ConsumeIndex()));
    }

    private void IndexEquals() {
        _evals.Add(new Evaluator.IndexEquals(ConsumeIndex()));
    }

    private int ConsumeIndex()
    {
        string indexS = _tq.ChompTo(")").Trim();

        int index;
        if (!int.TryParse(indexS, out index))
        {
            throw new Exception("Index must be numeric");
        }

        return index;
    }

    // pseudo selector :has(el)
    private void Has() {
        _tq.Consume(":has");
        
        string subQuery = _tq.ChompBalanced('(', ')');

        if (string.IsNullOrEmpty(subQuery))
        {
            throw new Exception(":has(el) subselect must not be empty");
        }
        
        _evals.Add(new StructuralEvaluator.Has(Parse(subQuery)));
    }

    // pseudo selector :contains(text), containsOwn(text)
    private void Contains(bool own) {
        _tq.Consume(own ? ":containsOwn" : ":contains");

        string searchText = TokenQueue.Unescape(_tq.ChompBalanced('(', ')'));

        if (string.IsNullOrEmpty(searchText))
        {
            throw new Exception(":contains(text) query must not be empty");
        }

        if (own)
        {
            _evals.Add(new Evaluator.ContainsOwnText(searchText));
        }
        else
        {
            _evals.Add(new Evaluator.ContainsText(searchText));
        }
    }

    // :matches(regex), matchesOwn(regex)
    private void Matches(bool own) {
        _tq.Consume(own ? ":matchesOwn" : ":matches");

        string regex = _tq.ChompBalanced('(', ')'); // don't unescape, as regex bits will be escaped

        if (string.IsNullOrEmpty(regex))
        {
            throw new Exception(":matches(regex) query must not be empty");
        }

        if (own)
        {
            _evals.Add(new Evaluator.MatchesOwn(new Regex(regex)));
        }
        else
        {
            _evals.Add(new Evaluator.MatchesRegex(new Regex(regex)));
        }
    }

    // :not(selector)
    private void Not() {
        _tq.Consume(":not");

        string subQuery = _tq.ChompBalanced('(', ')');

        if (string.IsNullOrEmpty(subQuery))
        {
            throw new Exception(":not(selector) subselect must not be empty");
        }

        _evals.Add(new StructuralEvaluator.Not(Parse(subQuery)));
    }
}

}
