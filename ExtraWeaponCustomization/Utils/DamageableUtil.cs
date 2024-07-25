using static Weapon;
using UnityEngine;

namespace ExtraWeaponCustomization.Utils
{
    internal static class DamageableUtil
    {
        public static IDamageable? GetDamageableFromData(WeaponHitData data)
        {
            return GetDamageableFromRayHit(data.rayHit);
        }

        public static IDamageable? GetDamageableFromCollider(Collider collider)
        {
            GameObject? gameObject = collider.gameObject;
            if (gameObject == null) return null;

            IDamageable? colliderDamageable = gameObject.GetComponent<ColliderMaterial>()?.Damageable;
            if (colliderDamageable != null)
                return colliderDamageable;

            return gameObject.GetComponent<IDamageable>();
        }

        public static IDamageable? GetDamageableFromRayHit(RaycastHit rayHit) => GetDamageableFromCollider(rayHit.collider);
    }
}
