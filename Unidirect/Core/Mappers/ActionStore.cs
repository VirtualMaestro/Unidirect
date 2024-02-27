using System;
using System.Runtime.CompilerServices;
using Unidirect.Core.Common;

namespace Unidirect.Core.Mappers
{
    public static class ActionStore<T> 
    {
        private static T _instance;
        private static IReset<T> _resetInstance;
        private static readonly int _actionId = ActionUniqueId.GetId;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ActionId()
        {
            return _actionId;
        }
        public static bool Has => _instance != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reset()
        {
            _resetInstance?.Reset();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get()
        {
            if (_instance == null)
                _Create();
            
            return _instance;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _Create()
        {
            _instance = Activator.CreateInstance<T>();
            _resetInstance = _instance as IReset<T>;
        }
    }
    
    internal static class ActionUniqueId
    {
        private static int _id;
        public static int GetId => _id++;
    }
}