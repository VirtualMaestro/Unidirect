using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Unidirect.Helpers
{
    public static class EventBus
    {
        private static readonly Dictionary<IEventChannelGeneric, IEventChannelGeneric> Channels = new(10);

        public static void Register(IEventChannelGeneric channel)
        {
            Channels.TryAdd(channel, channel);
        }
        
        public static void Unregister(IEventChannelGeneric channel)
        {
            if (Channels.TryGetValue(channel, out var genericChannel))
                Channels.Remove(genericChannel);
        }
        
        public static void ClearAll(bool purgePools = false)
        {
            foreach (var channel in Channels)
                channel.Value.Clear(purgePools);
        }

        public static void DisposeALl()
        {
            foreach (var channel in Channels)
                channel.Value.Dispose();
        }
    }
    
    public static class EventBus<T>
    {
        private static IEventChannel<T> _instance;
        private static UStack<T> _events = new(2, 0.5f);

        public static T GetEvent()
        {
            if (!_events.IsEmpty)
                return _events.Pop();

            return Activator.CreateInstance<T>();
        }
        
        public static int NumListeners => _IsChannelExist() ? _instance.Count : 0; 

        public static void AddListener(Action<T> listener)
        {
            _GetChannel().Add(listener);
        }

        public static void Dispatch(ref T t)
        {
            if (_IsChannelExist())
                _instance.Dispatch(ref t);

            if (_instance.IsEventReset)
                ((IEventReset) t).Reset();
            
            _events.Push(ref t);
        }

        public static void Dispatch(T t)
        {
            if (_IsChannelExist())
                _instance.Dispatch(ref t);

            if (_instance.IsEventReset)
                ((IEventReset) t).Reset();
            
            _events.Push(ref t);
        }

        public static void RemoveListener(Action<T> listener)
        {
            if (_IsChannelExist())
                _instance.Remove(listener);
        }

        public static void Clear(bool purgePool = false)
        {
            if (_IsChannelExist())
                _instance.Clear(purgePool);    
        }

        public static void Dispose()
        {
            if (_IsChannelExist())
            {
                EventBus.Unregister(_instance);
                _instance.Dispose();
                _instance = null;
                _events.Clear();
                _events = null;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool _IsChannelExist() => _instance != null && !_instance.IsDisposed;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IEventChannel<T> _GetChannel()
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new EventChannel<T>();
                EventBus.Register(_instance);
            }

            return _instance;
        } 
    }

    internal class EventChannel<T> : IEventChannel<T>
    {
        private UDLL<Action<T>> _handlers = new();

        public int Count => _handlers?.Count ?? 0;
        public bool IsDisposed => _handlers == null;

        public bool IsEventReset { get; } = typeof(T) is IEventReset ;
        
        public void Add(Action<T> handler)
        {
            _handlers.AddLast(handler);
        }

        public void Remove(Action<T> handler)
        {
            _handlers.Remove(handler);
        }

        public bool Contains(Action<T> handler)
        {
            return _handlers.Contains(handler);
        }

        public void Clear(bool purgePool = false)
        {
            _handlers.Clear(purgePool);
        }

        public void Dispatch(ref T t)
        {
            var node = _handlers.First;
            
            while (node != null)
            {
                var current = node;
                node = node.Next;
                
                current.Value(t);
            }
        }
        
        public void Dispose()
        {
            Clear(true);

            _handlers = null;
        }
    }
    
    internal interface IEventChannel<T> : IEventChannelGeneric
    {
        void Add(Action<T> handler);
        void Remove(Action<T> handler);
        void Dispatch(ref T t);
        bool Contains(Action<T> handler);
        bool IsEventReset { get; }
    }

    public interface IEventChannelGeneric
    {
        int Count { get; }
        void Clear(bool purgePool = false);
        void Dispose();
        bool IsDisposed { get; }
    }

    public interface IEventReset
    {
        public void Reset();
    }
}