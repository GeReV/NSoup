using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NSoup.Helper
{

    /// <summary>
    /// Provides a descending iterator and other 1.6 methods to allow support on the 1.5 JRE.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DescendableLinkedList<T> : LinkedList<T> where T : class
    {

        /// <summary>
        /// Create a new DescendableLinkedList.
        /// </summary>
        public DescendableLinkedList()
            : base()
        {
        }

        /// <summary>
        /// Get an iterator that starts and the end of the list and works towards the start.
        /// </summary>
        /// <returns>an iterator that starts and the end of the list and works towards the start.</returns>
        public IEnumerator<T> GetDescendingEnumerator()
        {
            return new DescendingEnumerator<T>(this);
        }

        private class DescendingEnumerator<V> : IEnumerator<V> where V : class
        {
            private LinkedList<V> list;
            private LinkedListNode<V> curr = null;

            private bool first = true;

            public DescendingEnumerator(LinkedList<V> list)
            {
                this.list = list;
            }

            #region IEnumerator<V> Members

            public V Current
            {
                get { return curr == null ? null : curr.Value; }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get { return curr == null ? null : curr.Value; }
            }

            public bool MoveNext()
            {
                if (first)
                {
                    first = false;
                    curr = list.Last;

                    return curr != null;
                }

                if (curr.Previous == null)
                {
                    return false;
                }

                curr = curr.Previous;
                return true;
            }

            public void Reset()
            {
                first = true;
            }

            #endregion
        }
    }
}