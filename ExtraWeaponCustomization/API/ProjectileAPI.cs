using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;
using System;

namespace EWC.API
{
    public static class ProjectileAPI
    {
        public static event Action<EWCProjectileComponentBase>? OnProjectileSpawned;
        public static event Action<EWCProjectileComponentBase>? OnProjectileDestroyed;

        internal static void FireProjectileSpawnedCallback(EWCProjectileComponentBase projectile) => OnProjectileSpawned?.Invoke(projectile);
        internal static void FireProjectileDestroyedCallback(EWCProjectileComponentBase projectile) => OnProjectileDestroyed?.Invoke(projectile);
    }
}
