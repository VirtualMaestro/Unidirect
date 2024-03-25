using System;
using Unidirect.Core.View;

namespace Unidirect.Core.Mappers
{
    public static class MediatorStore<TMediator> where TMediator: Mediator
    {
        private static TMediator _instance;

        public static Mediator Get(out bool isCreated)
        {
            isCreated = _instance == null || _instance.IsDisposed;

            if (isCreated)
            {
                _instance = Activator.CreateInstance<TMediator>();
            }
                
            return _instance;
        }

        public static void Clear()
        {
            _instance = default;
        }
    }
}