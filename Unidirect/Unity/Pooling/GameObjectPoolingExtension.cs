using UnityEngine;

namespace Unidirect.Unity.Pooling
{
    public static class GameObjectPoolingExtension
    {
        public static void Destroy(this GameObject instance)
        {
            if (instance.TryGetComponent<PoolableMono>(out var poolableMono) && poolableMono.Pool != null)
                poolableMono.Pool.Put(instance);
            else 
                Object.Destroy(instance);
        }
    }
}