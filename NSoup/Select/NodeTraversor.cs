using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Select
{
    /**
     * Breadth first node traversor.
     */
    internal class NodeTraversor
    {
        private NodeVisitor visitor;

        public NodeTraversor(NodeVisitor visitor)
        {
            this.visitor = visitor;
        }

        public void Traverse(Node root)
        {
            Node node = root;
            int depth = 0;

            while (node != null)
            {
                visitor.Head(node, depth);
                if (node.ChildNodes.Count > 0)
                {
                    node = node.ChildNodes[0];
                    depth++;
                }
                else
                {
                    while (node.NextSibling == null && depth > 0)
                    {
                        visitor.Tail(node, depth);
                        node = node.ParentNode;
                        depth--;
                    }
                    visitor.Tail(node, depth);
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
