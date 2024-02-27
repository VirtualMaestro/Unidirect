using System;
using System.Runtime.CompilerServices;
using Unidirect.Core.Logic;

namespace Unidirect.Core.Mappers
{
    public static class ActionMapper<TAction, TModel> 
    {
        private static ISystem<TAction, TModel> _system;
        private static Action<TAction> _handler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MapSystem<TSystem>() where TSystem : ISystem<TAction, TModel>
        {
            _system ??= Activator.CreateInstance<TSystem>();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasSystem() => _system != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ISystem<TAction, TModel> GetSystem()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_system == null)
                throw new Exception($"There is no system mapped to {typeof(TAction)}. Use MapSystem method for this.");
#endif
            return _system;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MapHandler(Action<TAction> handler)
        {
            _handler = handler;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasHandler() => _handler != null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Action<TAction> GetHandler()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_handler == null)
                throw new Exception($"There is no handler mapped to {typeof(TAction)}. Use MapHandler method for this.");
#endif
            return _handler;
        }

        public static void Clear()
        {
            _system = null;
            _handler = null;
        }
    }
}