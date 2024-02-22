using System;
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
    public class UStack<T>
    {
        private T[] _storage;
        private int _freeIndex;
        private int _size;

        /// <summary>
        /// How fast stack will grow by resizing. By default every resize will be doubled.
        /// </summary>
        public float ResizeModifier { set; get; }
        /// <summary>
        /// If stack is full.
        /// </summary>
        public bool IsFull => _freeIndex == _size;
        /// <summary>
        /// If stack is empty.
        /// </summary>
        public bool IsEmpty => _freeIndex == 0;
        /// <summary>
        /// Available free spots in the stack.
        /// </summary>
        public int Available => _size - _freeIndex;
        /// <summary>
        /// How many items in stack.
        /// </summary>
        public int Count => _freeIndex;
        /// <summary>
        /// If this stack able to resize.
        /// </summary>
        public bool IsResizable;
        
        public UStack(int capacity = 2, float resizeModifier = 2.0f, bool isResizable = true)
        {
            IsResizable = isResizable;
            ResizeModifier = resizeModifier <= 0 ? 0.5f : resizeModifier;
            capacity = capacity > 0 ? capacity : 1;
            _storage = new T[capacity];
            _size = capacity;
        }

        public bool Push(ref T item)
        {
            if (_freeIndex == _size)
            {
                if (!IsResizable)
                    return false;
                
                _Resize(_GetResizeSize());
            }

            _storage[_freeIndex++] = item;
            return true;
        }

        public bool Push(T item)
        {
            if (_freeIndex == _size)
            {
                if (!IsResizable)
                    return false;
                
                _Resize(_GetResizeSize());
            }

            _storage[_freeIndex++] = item;
            return true;
        }

        public ref T Pop()
        {
            return ref _storage[--_freeIndex];
        }

        /// <summary>
        /// Allocates free spots for a new objects.
        /// It will not take into account occupied spots and if needed will extend the storage.
        /// </summary>
        public void Allocate(int num)
        {
            var allocNumSpots = Available - num;

            if (allocNumSpots < 0)
                _Resize(-allocNumSpots);
        }

        public void Clear()
        {
            Array.Clear(_storage, 0, _size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int _GetResizeSize()
        {
            var newSize = (int)(_size + _size * ResizeModifier % _size);
            newSize = newSize == _size ? newSize + 1 : newSize;
            return newSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void _Resize(int newSize)
        {
            var newOne = new T[newSize];
            Array.Copy(_storage, newOne, _size);
            _storage = newOne;
            _size = newSize;
        }
    }
}