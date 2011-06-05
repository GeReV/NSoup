using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Select
{

/// <summary>
/// Base structural evaluator.
/// </summary>
    abstract class StructuralEvaluator : Evaluator
    {
        Evaluator _evaluator;

        public class Root : Evaluator
        {
            public override bool Matches(Element root, Element element)
            {
                return root == element;
            }
        }

        public class Has : StructuralEvaluator
        {
            public Has(Evaluator evaluator)
            {
                this._evaluator = evaluator;
            }

            public override bool Matches(Element root, Element element)
            {
                foreach (Element e in element.GetAllElements())
                {
                    if (e != element && _evaluator.Matches(root, e))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override string ToString()
            {
                return string.Format(":has({0})", _evaluator);
            }
        }

        public class Not : StructuralEvaluator
        {
            public Not(Evaluator evaluator)
            {
                this._evaluator = evaluator;
            }

            public override bool Matches(Element root, Element node)
            {
                return !_evaluator.Matches(root, node);
            }

            public override string ToString()
            {
                return string.Format(":not{0}", _evaluator);
            }
        }

        public class Parent : StructuralEvaluator
        {
            public Parent(Evaluator evaluator)
            {
                this._evaluator = evaluator;
            }

            public override bool Matches(Element root, Element element)
            {
                if (root == element)
                {
                    return false;
                }

                Element parent = element.Parent;

                while (parent != root)
                {
                    if (_evaluator.Matches(root, parent))
                    {
                        return true;
                    }
                    parent = parent.Parent;
                }

                return false;
            }

            public override string ToString()
            {
                return string.Format(":parent{0}", _evaluator);
            }
        }

        public class ImmediateParent : StructuralEvaluator
        {
            public ImmediateParent(Evaluator evaluator)
            {
                this._evaluator = evaluator;
            }

            public override bool Matches(Element root, Element element)
            {
                if (root == element)
                {
                    return false;
                }

                Element parent = element.Parent;
                return parent != null && _evaluator.Matches(root, parent);
            }
        }

        public class PreviousSibling : StructuralEvaluator
        {
            public PreviousSibling(Evaluator evaluator)
            {
                this._evaluator = evaluator;
            }

            public override bool Matches(Element root, Element element)
            {
                if (root == element)
                {
                    return false;
                }

                Element prev = element.PreviousElementSibling;

                while (prev != null)
                {
                    if (_evaluator.Matches(root, prev))
                    {
                        return true;
                    }

                    prev = prev.PreviousElementSibling;
                }

                return false;
            }

            public override string ToString()
            {
                return string.Format(":prev*{0}", _evaluator);
            }
        }

        public class ImmediatePreviousSibling : StructuralEvaluator
        {
            public ImmediatePreviousSibling(Evaluator evaluator)
            {
                this._evaluator = evaluator;
            }

            public override bool Matches(Element root, Element element)
            {
                if (root == element)
                {
                    return false;
                }

                Element prev = element.PreviousElementSibling;

                return prev != null && _evaluator.Matches(root, prev);
            }

            public override string ToString()
            {
                return string.Format(":prev{0}", _evaluator);
            }
        }
    }
}
