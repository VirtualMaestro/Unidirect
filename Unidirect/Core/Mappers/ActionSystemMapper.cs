using System;
using System.Runtime.CompilerServices;
using Unidirect.Core.Logic;

namespace Unidirect.Core.Mappers
{
    public static class ActionSystemMapper<TLogicAction, TModel> 
    {
        private static ISystem<TLogicAction, TModel> _instance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Map<TSystem>() where TSystem : ISystem<TLogicAction, TModel>
        {
            _instance ??= Activator.CreateInstance<TSystem>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ISystem<TLogicAction, TModel> Get()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_instance == null)
                throw new Exception($"There is no system mapped to {typeof(TLogicAction)}. Use Map method for this.");
#endif
            return _instance;
        }

        public static void Clear()
        {
            _instance = null;
        }
    }
}