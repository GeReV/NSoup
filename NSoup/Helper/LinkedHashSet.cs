using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace NSoup
{
    public class LinkedHashSet<T> : ICollection<T>, IEnumerable<T>, IEnumerable
    {
        private HashSet<T> _hashSet;
        private List<T> _list;

        /// <summary>
        /// Initializes a new instance of the NSoup.LinkedHashSet<T> 
        /// that is empty and has the default initial capacity.
        /// </summary>
        public LinkedHashSet()
        {
            _hashSet = new HashSet<T>();
            _list = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the NSoup.LinkedHashSet<T> class
        /// that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new list can initially store.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">capacity is less than 0.</exception>
        public LinkedHashSet(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }

            _hashSet = new HashSet<T>();
            _list = new List<T>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the NSoup.LinkedHashSet<T> class
        /// that contains elements copied from the specified collection and has sufficient
        /// capacity to accommodate the number of elements copied.
        /// </summary>
        /// <param name="c">The collection whose elements are copied to the new list.</param>
        /// <exception cref="System.ArgumentNullException">collection is null.</exception>
        public LinkedHashSet(IEnumerable<T> collection)
            : this()
        {
            Initialize(collection);
        }

        /// <summary>
        /// Initializes a new instance of the NSoup.LinkedHashSet<T> class
        /// that is empty and uses the specified equality comparer for the set type.
        /// </summary>
        /// <param name="comparer">
        /// The System.Collections.Generic.IEqualityComparer<T> implementation to use
        /// when comparing values in the set, or null to use the default System.Collections.Generic.EqualityComparer<T>
        /// implementation for the set type.
        /// </param>
        public LinkedHashSet(IEqualityComparer<T> comparer)
        {
            _hashSet = new HashSet<T>(comparer);
            _list = new List<T>();
        }

        /// <summary>
        /// Initializes a new instance of the NSoup.LinkedHashSet<T> class
        /// that uses the specified equality comparer for the set type, contains elements
        /// copied from the specified collection, and has sufficient capacity to accommodate
        /// the number of elements copied.   
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new set.</param>
        /// <param name="comparer">
        /// The System.Collections.Generic.IEqualityComparer<T> implementation to use
        /// when comparing values in the set, or null to use the default System.Collections.Generic.EqualityComparer<T>
        /// implementation for the set type.
        /// </param>
        /// <exception cref="System.ArgumentNullException">collection is null.</exception>
        public LinkedHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
            : this(comparer)
        {
            Initialize(collection);
        }


        private void Initialize(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentException("collection");
            }

            foreach (T item in collection)
            {
                this.Add(item);
            }
        }

        public bool Add(T item)
        {
            if (_hashSet.Add(item))
            {
                _list.Add(item);
                return true;
            }
            return false;
        }

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }

        /// <summary>
        /// Removes all elements from the NSoup.LinkedHashSet<T>.
        /// </summary>
        public void Clear()
        {
            _hashSet.Clear();
            _list.Clear();
        }

        /// <summary>
        /// Determines whether a System.Collections.Generic.HashSet<T> object contains
        /// the specified element.
        /// </summary>
        /// <param name="item">The element to locate in the NSoup.LinkedHashSet<T> object.</param>
        /// <returns>true if the System.Collections.Generic.HashSet<T> object contains the specified element; otherwise, false.</returns>
        public bool Contains(T item)
        {
            return _hashSet.Contains(item);
        }

        
        /// <summary>
        /// Copies the elements of a NSoup.LinkedHashSet<T> object to 
        /// an array, starting at the specified array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional array that is the destination of the elements copied 
        /// from the System.Collections.Generic.HashSet<T> object. The array must have 
        /// zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <exception cref="System.ArgumentNullException">array is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">arrayIndex is less than 0.</exception>
        /// <exception cref="System.ArgumentException">arrayIndex is greater than the length of the destination array.  -or- count is larger than the size of the destination array.</exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of elements actually contained in the System.Collections.Generic.List<T>.
        /// </summary>
        public int Count
        {
            get { return _hashSet.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the NSoup.LinkedHashSet<T>.
        /// </summary>
        /// <param name="item">The object to remove from the System.Collections.Generic.List<T>. The value can be null for reference types.</param>
        /// <returns>
        /// true if item is successfully removed; otherwise, false. This method also 
        /// returns false if item was not found in the NSoup.LinkedHashSet<T>.
        /// </returns>
        public bool Remove(T item)
        {
            return _hashSet.Remove(item) || _list.Remove(item);
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Gets the System.Collections.Generic.IEqualityComparer<T> object that is used 
        /// to determine equality for the values in the set.
        /// </summary>
        /// <returns>
        /// The System.Collections.Generic.IEqualityComparer<T> object that is used to
        /// determine equality for the values in the set.
        /// </returns>
        public IEqualityComparer<T> Comparer
        {
            get { return _hashSet.Comparer; }
        }

        /// <summary>
        /// Sets the capacity of a NSoup.LinkedHashSet<T> object to the
        /// actual number of elements it contains, rounded up to a nearby, implementation-specific
        /// value.
        /// </summary>
        public void TrimExcess()
        {
            _hashSet.TrimExcess();
            _list.TrimExcess();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _hashSet.IntersectWith(other);
            _list = _list.Intersect(other).ToList(); // Hopefully this keeps everything in the same order.
        }
    }
}
