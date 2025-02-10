using EWC.CustomWeapon.Properties.Effects;
using UnityEngine;

namespace EWC.API
{
    public static class ExplosionAPI
    {
        public delegate void ExplosionSpawnedCallback(Vector3 position, Explosive property);

        public static event ExplosionSpawnedCallback? OnExplosionSpawned;

        internal static void FireExplosionSpawnedCallback(Vector3 position, Explosive property) => OnExplosionSpawned?.Invoke(position, property);
    }
}
