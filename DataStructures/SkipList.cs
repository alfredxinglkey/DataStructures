﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace DataStructures
{
    public class SkipList<T> : ICollection<T>
        where T : IComparable<T>
    {
        private const byte MAX_HEIGHT = 32;
        internal const byte HEIGHT_STEP = 4;
        private readonly IComparer<T> _comparer;

        internal readonly Node _head;
        private readonly Random _random = new Random();
        internal readonly Node _tail;

        internal byte _height;
        protected internal Node _lastFoundNode;

        public SkipList(IComparer<T> comparer = null)
        {
            _comparer = comparer ?? Comparer<T>.Default;
            _random = new Random();

            _head = new Node(default(T), HEIGHT_STEP);
            _tail = new Node(default(T), HEIGHT_STEP);
            Reset();
        }

        /// <summary>
        /// Remove all items from the collection.
        /// </summary>
        public virtual void Clear()
        {
            _head.SetHeight(HEIGHT_STEP);
            _tail.SetHeight(HEIGHT_STEP);
            Reset();
        }

        /// <summary>
        /// Check if the given item is present in the collection. Equality check is done using the provided or default comparer.
        /// </summary>
        /// <param name="item">Item to check.</param>
        /// <returns>True, if collection contains the item, else - False.</returns>
        public virtual bool Contains(T item)
        {
            Node node = FindNode(item);

            _lastFoundNode = node;

            return CompareNode(node, item) == 0;
        }

        /// <summary>
        /// Number of elements in collection.
        /// </summary>
        public int Count { get; private set; }

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Add a given item to the collection.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public virtual void Add(T item)
        {
            Node prev = FindNode(item);

            _lastFoundNode = AddNewNode(item, prev);
        }

        /// <summary>
        /// Remove a given item from the collection (if it's present).
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True, if item was found and deleted, else - False.</returns>
        public virtual bool Remove(T item)
        {
            Node node = FindNode(item);
            if (CompareNode(node, item) != 0) return false;

            DeleteNode(node);
            if (_lastFoundNode == node)
            {
                _lastFoundNode = _head;
            }

            return true;
        }

        /// <summary>
        /// Return the first item in the collection, without removing it.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="InvalidOperationException"/> is thrown if collection is empty.</exception>
        /// <returns>First item in the collection.</returns>
        public virtual T Peek()
        {
            if (Count == 0) throw new InvalidOperationException("Collection is empty.");

            return _head.GetNext(0).Item;
        }

        /// <summary>
        /// Return the first item in the collection, without removing it.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="InvalidOperationException"/> is thrown if collection is empty.</exception>
        /// <returns>First item in the collection.</returns>
        public virtual T GetFirst()
        {
            return Peek();
        }

        /// <summary>
        /// Return the last item in the collection, without removing it.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="InvalidOperationException"/> is thrown if collection is empty.</exception>
        /// <returns>Last item in the collection.</returns>
        public virtual T GetLast()
        {
            if (Count == 0) throw new InvalidOperationException("Collection is empty.");

            return _tail.GetPrev(0).Item;
        }

        /// <summary>
        /// Return the first item in the collection and remove it.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="InvalidOperationException"/> is thrown if collection is empty.</exception>
        /// <returns>First item in the collection.</returns>
        public virtual T Take()
        {
            if (Count == 0) throw new InvalidOperationException("Collection is empty.");

            Node node = _head.GetNext(0);
            DeleteNode(node);
            if (_lastFoundNode == node)
            {
                _lastFoundNode = _head;
            }
            return node.Item;
        }

        /// <summary>
        /// Return the last item in the collection and remove it.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="InvalidOperationException"/> is thrown if collection is empty.</exception>
        /// <returns>Last item in the collection.</returns>
        public virtual T TakeLast()
        {
            if (Count == 0) throw new InvalidOperationException("Collection is empty.");

            Node node = _tail.GetPrev(0);
            DeleteNode(node);
            if (_lastFoundNode == node)
            {
                _lastFoundNode = _head;
            }
            return node.Item;
        }

        public virtual IEnumerator<T> GetEnumerator()
        {
            var items = new LinkedList<T>();
            Node node = _head.GetNext(0);
            while (node != _tail)
            {
                items.AddLast(node.Item);
                node = node.GetNext(0);
            }
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            CopyTo((Array)array, arrayIndex);
        }

        public virtual void CopyTo(Array array, int arrayIndex)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException("arrayIndex");

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Insufficient space in destination array.");

            Node node = _head.GetNext(0);
            for (int i = arrayIndex; i < arrayIndex + Count; i++)
            {
                array.SetValue(node.Item, i);
                node = node.GetNext(0);
            }
        }

        private void Reset()
        {
            for (int i = 0; i < _head.Height; i++)
            {
                _head.SetNext(i, _tail);
                _tail.SetPrev(i, _head);
            }

            Count = 0;
            _height = 1;
            _lastFoundNode = _head;
        }

        protected Node FindNode(T key)
        {
            int level = _height - 1;
            Node node = _head;
            Node lastFound = _lastFoundNode;

            int cmp;
            if (lastFound != _head)
            {
                if ((cmp = CompareNode(lastFound, key)) == 0) return lastFound;
                if (cmp < 0)
                {
                    node = lastFound;
                    level = lastFound.Height - 1;
                }
            }

            while (level >= 0)
            {
                Node next = node.GetNext(level);
                while ((cmp = CompareNode(next, key)) < 0)
                {
                    node = next;
                    next = next.GetNext(level);
                }
                if (cmp == 0)
                {
                    node = next;
                    break;
                }

                level--;
            }

            return node;
        }

        protected Node AddNewNode(T item, Node prev)
        {
            Node next = prev.GetNext(0);
            byte newNodeHeight = GetNewNodeHeight();

            var newNode = new Node(item, newNodeHeight);
            InsertNode(newNode, newNodeHeight, prev, next);
            Count++;
            return newNode;
        }

        private byte GetNewNodeHeight()
        {
            byte maxNodeHeight = _height;
            if (maxNodeHeight < MAX_HEIGHT && (1 << maxNodeHeight) < Count)
            {
                maxNodeHeight++;
            }
            var nodeHeight = (byte) (1 + _random.Next(maxNodeHeight));
            if (nodeHeight > _height)
            {
                _height = nodeHeight;
                if (_head.Height < _height)
                {
                    maxNodeHeight = (byte) _head.Height;
                    _head.SetHeight(maxNodeHeight + HEIGHT_STEP);
                    _tail.SetHeight(maxNodeHeight + HEIGHT_STEP);
                    while (maxNodeHeight < _head.Height)
                    {
                        _head.SetNext(maxNodeHeight, _tail);
                        _tail.SetPrev(maxNodeHeight, _head);
                        maxNodeHeight++;
                    }
                }
            }
            return nodeHeight;
        }

        private static void InsertNode(Node newNode, byte height, Node prev, Node next)
        {
            for (int i = 0; i < height; i++)
            {
                while (prev.Height <= i) prev = prev.GetPrev(i - 1);
                while (next.Height <= i) next = next.GetNext(i - 1);
                newNode.SetPrev(i, prev);
                newNode.SetNext(i, next);

                prev.SetNext(i, newNode);
                next.SetPrev(i, newNode);
            }
        }

        protected void DeleteNode(Node node)
        {
            for (byte i = 0; i < node.Height; i++)
            {
                Node prev = node.GetPrev(i);
                Node next = node.GetNext(i);

                while (prev.Height <= i) prev = prev.GetPrev(i - 1);
                while (next.Height <= i) next = next.GetNext(i - 1);

                prev.SetNext(i, next);
                next.SetPrev(i, prev);
            }

            Count--;

            if (_height > 1 && (1 << _height) > Count)
            {
                _height--;
            }
        }

        protected int CompareNode(Node node, T key)
        {
            if (node == _head) return -1;
            if (node == _tail) return 1;

            return _comparer.Compare(node.Item, key);
        }

        [DebuggerDisplay("Node [{Item}] ({Height})")]
        protected internal class Node
        {
            private readonly T _item;
            private Node[] _next;
            private Node[] _prev;

            protected internal Node(T item, byte height)
            {
                _item = item;
                _next = new Node[height];
                _prev = new Node[height];
            }

            protected internal T Item
            {
                get { return _item; }
            }

            protected internal int Height
            {
                get { return _next.Length; }
            }

            protected internal Node GetNext(int level)
            {
                return _next[level];
            }

            protected internal void SetNext(int level, Node node)
            {
                _next[level] = node;
            }

            protected internal void SetPrev(int level, Node node)
            {
                _prev[level] = node;
            }

            protected internal Node GetPrev(int level)
            {
                return _prev[level];
            }

            protected internal void SetHeight(int height)
            {
                var newNext = new Node[height];
                var newPrev = new Node[height];
                int count = Math.Min(_next.Length, height);
                for (int i = 0; i < count; i++)
                {
                    newNext[i] = _next[i];
                    newPrev[i] = _prev[i];
                }
                _next = newNext;
                _prev = newPrev;
            }
        }
    }
}