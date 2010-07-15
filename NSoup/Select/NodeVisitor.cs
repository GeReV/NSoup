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
    public interface NodeVisitor
    {
        void Head(Node node, int depth);
        void Tail(Node node, int depth);
    }
}
