using EWC.CustomWeapon.Properties.Effects;
using System;
using UnityEngine;

namespace EWC.API
{
    public static class ExplosionAPI
    {
        public static event Action<Vector3, Explosive>? OnExplosionSpawned;

        internal static void FireExplosionSpawnedCallback(Vector3 position, Explosive property) => OnExplosionSpawned?.Invoke(position, property);
    }
}
