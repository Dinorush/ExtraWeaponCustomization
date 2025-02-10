using EWC.CustomWeapon.Properties.Traits.CustomProjectile.Components;

namespace EWC.API
{
    public static class ProjectileAPI
    {
        public delegate void ProjectileCallback(EWCProjectileComponentBase projectile);
        public delegate void ProjectileHitCallback(EWCProjectileComponentBase projectile, IDamageable? damageable);

        public static event ProjectileCallback? OnProjectileSpawned;
        public static event ProjectileHitCallback? OnProjectileHit;
        public static event ProjectileCallback? OnProjectileDestroyed;

        internal static void FireProjectileSpawnedCallback(EWCProjectileComponentBase projectile) => OnProjectileSpawned?.Invoke(projectile);
        internal static void FireProjectileHitCallback(EWCProjectileComponentBase projectile, IDamageable? damageable) => OnProjectileHit?.Invoke(projectile, damageable);
        internal static void FireProjectileDestroyedCallback(EWCProjectileComponentBase projectile) => OnProjectileDestroyed?.Invoke(projectile);
    }
}
