using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Select
{
    /// <summary>
    /// Base combining (and, or) evaluator.
    /// </summary>
    internal abstract class CombiningEvaluator : Evaluator
    {
        protected readonly List<Evaluator> _evaluators;

        private CombiningEvaluator()
            : base()
        {
            _evaluators = new List<Evaluator>();
        }

        private CombiningEvaluator(ICollection<Evaluator> evaluators)
            : this()
        {
            this._evaluators.AddRange(evaluators);
        }

        public Evaluator RightMostEvaluator()
        {
            return _evaluators.Count > 0 ? _evaluators[_evaluators.Count - 1] : null;
        }

        public void ReplaceRightMostEvaluator(Evaluator replacement)
        {
            _evaluators[_evaluators.Count - 1] = replacement;
        }

        public List<Evaluator> Evaluators
        {
            get { return _evaluators; }
        }

        public sealed class And : CombiningEvaluator
        {
            public And(ICollection<Evaluator> evaluators)
                : base(evaluators)
            {
            }

            public And(params Evaluator[] evaluators)
                : base(evaluators)
            {
            }

            public override bool Matches(Element root, Element node)
            {
                for (int i = 0; i < _evaluators.Count; i++)
                {
                    Evaluator s = _evaluators[i];
                    if (!s.Matches(root, node))
                    {
                        return false;
                    }
                }

                return true;
            }

            public override string ToString()
            {
                return string.Join(" ", _evaluators.Select(e => e.ToString()).ToArray());
            }
        }

        public sealed class Or : CombiningEvaluator
        {
            public Or(ICollection<Evaluator> evaluators)
                : base()
            {
                if (evaluators.Count > 1)
                {
                    this._evaluators.Add(new And(evaluators));
                }
                else // 0 or 1
                {
                    this._evaluators.AddRange(evaluators);
                }
            }

            public Or()
                : base()
            {}

            public void Add(Evaluator e)
            {
                _evaluators.Add(e);
            }

            public override bool Matches(Element root, Element node)
            {
                for (int i = 0; i < _evaluators.Count; i++)
                {
                    Evaluator s = _evaluators[i];
                    if (s.Matches(root, node))
                    {
                        return true;
                    }
                }
                return false;
            }

            public override string ToString()
            {
                return string.Format(":or{0}", _evaluators);
            }
        }
    }
}