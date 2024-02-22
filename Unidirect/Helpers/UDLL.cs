using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#if ENABLE_IL2CPP
using Unity.IL2CPP.CompilerServices;
#endif

namespace Unidirect.Helpers
{
#if ENABLE_IL2CPP
     [UnityEngine.Scripting.Preserve]
     [Il2CppSetOption (Option.NullChecks, false)]
     [Il2CppSetOption (Option.ArrayBoundsChecks, false)]
#endif
    public sealed class UDLL<T>
    {
        private UStack<UDLLNode<T>> _pool;
        private Dictionary<T, UDLLNode<T>> _values;
        private int _count;
        public int Count => _count;
        public UDLLNode<T> First;
        public UDLLNode<T> Last;

        public UDLL(int capacity = 8, float resizeModifier = 0.5f)
        {
            _pool = new UStack<UDLLNode<T>>(capacity, resizeModifier);
            _values = new Dictionary<T, UDLLNode<T>>(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UDLLNode<T> AddLast(T value)
        {
            if (!_TryAdd(value, out var node))
                return null;

            if (Last == node)
                return node;
            
            Last.Next = node;
            node.Prev = Last;
            Last = node;
             
            return node; 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UDLLNode<T> AddFirst(T value)
        {
            if (!_TryAdd(value, out var node))
                return null;

            if (First == node)
                return node;
            
            First.Prev = node;
            node.Next = First;
            First = node;
            
            return node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool _TryAdd(T value, out UDLLNode<T> node)
        {
            node = null;
            
            if (_values.ContainsKey(value))
                return false;
            
            node = _GetNode();
            node.Value = value;
            
            if (First == null)
            {
                First = node;
                Last = node;
            }
            
            _values.Add(value, node);
            _count++;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(T value)
        {
            if (_values.TryGetValue(value, out var node))
                Remove(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(UDLLNode<T> node)
        {
            if (node == First)
            {
                First = First.Next;
                
                if (Count > 1)
                    First.Prev = null;
            }
            else if (node == Last)
            {
                Last = Last.Prev;

                if (Count > 1)
                    Last.Next = null;
            }
            else
            {
                node.Prev.Next = node.Next;
                node.Next.Prev = node.Prev;
            }

            _Remove(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private UDLLNode<T> _GetNode()
        {
            return _pool.IsEmpty ? Activator.CreateInstance<UDLLNode<T>>() : _pool.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _Remove(UDLLNode<T> node)
        {
            _values.Remove(node.Value);
            node.Next = default;
            node.Prev = default;
            node.Value = default;
            _pool.Push(node);
            _count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T value)
        {
            return _values.ContainsKey(value);
        }

        public void Clear(bool purge = false)
        {
            var node = First;
            
            while (node != null)
            {
                var currentNode = node;
                node = node.Next;
                
                currentNode.Next = default;
                currentNode.Prev = default;
                currentNode.Value = default;

                if (!purge)
                    _pool.Push(currentNode);
            }
            
            _values.Clear();
            
            if (purge)
                _pool.Clear();
            
            _count = 0;
        }

        public void Dispose()
        {
            Clear(true);
            First = null;
            Last = null;
            _pool = null;
            _values = null;
        }
    }

    public sealed class UDLLNode<T>
    {
        public T Value;
        public UDLLNode<T> Prev;
        public UDLLNode<T> Next;
    }
}