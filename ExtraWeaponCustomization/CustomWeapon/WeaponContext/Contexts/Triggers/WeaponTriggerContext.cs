using Gear;
using static Weapon;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponTriggerContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponTriggerContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }

        public static IDamageable? GetDamageableFromData(WeaponHitData data)
        {
            return GetDamageableFromRayHit(data.rayHit);
        }

        public static IDamageable? GetDamageableFromRayHit(RaycastHit rayHit)
        {
            GameObject? gameObject = rayHit.collider.gameObject;
            if (gameObject == null) return null;

            IDamageable? collider = gameObject.GetComponent<ColliderMaterial>()?.Damageable;
            if (collider != null)
                return collider;

            return gameObject.GetComponent<IDamageable>();
        }
    }
}
