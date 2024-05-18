using Gear;
using static Weapon;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponTriggerContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public TriggerType Type { get; }

        public WeaponTriggerContext(BulletWeapon weapon, TriggerType type)
        {
            Weapon = weapon;
            Type = type;
        }

        public static IDamageable? GetDamageableFromData(WeaponHitData data)
        {
            GameObject? gameObject = data.rayHit.collider.gameObject;
            if (gameObject == null) return null;

            IDamageable? collider = gameObject.GetComponent<ColliderMaterial>()?.Damageable;
            if (collider != null)
                return collider;

            return gameObject.GetComponent<IDamageable>();
        }
    }
}
