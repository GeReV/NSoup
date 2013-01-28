using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Select
{
    /// <summary>
    /// Node visitor interface. Provide an implementing class to NodeTraversor to iterate through nodes.
    /// This interface provides two methods, Head() and Tail(). The head method is called when the node is first
    /// seen, and the tail method when all of the node's children have been visited. As an example, head can be used to
    /// create a start tag for a node, and tail to create the end tag.
    /// </summary>
    public interface NodeVisitor
    {
        /// <summary>
        /// Callback for when a node is first visited.
        /// </summary>
        /// <param name="node">The node being visited.</param>
        /// <param name="depth">The depth of the node, relative to the root node. E.g., the root node has depth 0, and a child node
        /// of that will have depth 1.</param>
        void Head(Node node, int depth);

        /// <summary>
        /// Callback for when a node is last visited, after all of its descendants have been visited.
        /// </summary>
        /// <param name="node">The node being visited.</param>
        /// <param name="depth">the depth of the node, relative to the root node. E.g., the root node has depth 0, and a child node
        /// of that will have depth 1.</param>
        void Tail(Node node, int depth);
    }
}
