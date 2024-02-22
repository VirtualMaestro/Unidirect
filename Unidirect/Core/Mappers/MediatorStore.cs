using System;
using Unidirect.Core.View;

namespace Unidirect.Core.Mappers
{
    public static class MediatorStore<TMediator> where TMediator: IMediator
    {
        public static bool IsStandAlone;
        private static TMediator _instance;

        public static IMediator Get(out bool isCreated)
        {
            isCreated = _instance == null;

            if (isCreated)
            {
                _instance = Activator.CreateInstance<TMediator>();
                _instance.IsStandAlone = IsStandAlone;
            }
                
            return _instance;
        }

        public static void Clear()
        {
            _instance = default;
        }
    }
}