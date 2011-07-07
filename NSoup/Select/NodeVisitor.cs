using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSoup.Nodes;

namespace NSoup.Select
{
    /**
     * Node visitor interface
     */
    internal interface NodeVisitor
    {
        void Head(Node node, int depth);
        void Tail(Node node, int depth);
    }
}
