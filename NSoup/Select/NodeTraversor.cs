using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Select
{
    /// <summary>
    /// Depth-first node traversor. Use to iterate through all nodes under and including the specified root node.
    /// This implementation does not use recursion, so a deep DOM does not risk blowing the stack.
    /// </summary>
    internal class NodeTraversor
    {
        private NodeVisitor _visitor;

        /// <summary>
        /// Create a new traversor.
        /// </summary>
        /// <param name="visitor">A class implementing the NodeVisitor interface, to be called when visiting each node.</param>
        public NodeTraversor(NodeVisitor visitor)
        {
            this._visitor = visitor;
        }

        /// <summary>
        /// Start a depth-first traverse of the root and all of its descendants.
        /// </summary>
        /// <param name="root">The root node point to traverse.</param>
        public void Traverse(Node root)
        {
            Node node = root;
            int depth = 0;

            while (node != null)
            {
                _visitor.Head(node, depth);
                if (node.ChildNodes.Count > 0)
                {
                    node = node.ChildNodes[0];
                    depth++;
                }
                else
                {
                    while (node.NextSibling == null && depth > 0)
                    {
                        _visitor.Tail(node, depth);
                        node = node.ParentNode;
                        depth--;
                    }
                    _visitor.Tail(node, depth);
                    if (node == root)
                    {
                        break;
                    }
                    node = node.NextSibling;
                }
            }
        }
    }
}
