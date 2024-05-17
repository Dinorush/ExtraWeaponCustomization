using ExtraWeaponCustomization.Utils;
using Gear;
using System;
using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitEnemyContext : WeaponTriggerContext
    {
        public WeaponHitData Data { get; }
        public float AdditionalDist { get; }
        public IDamageable Damageable { get; }
        public float Falloff { get; }

        public WeaponPreHitEnemyContext(ref WeaponHitData weaponHitData, float additionalDist, BulletWeapon weapon) : base(weapon, TriggerType.OnHit)
        {
            IDamageable? damageable = GetDamageableFromData(weaponHitData);
            if (damageable == null || damageable.GetBaseAgent()?.Type != Agents.AgentType.Enemy)
                throw new ArgumentNullException("Damageable", "WeaponPreHitEnemyContext must be called on an enemy damageable hit.");

            Data = weaponHitData;
            Damageable = damageable;
            AdditionalDist = additionalDist;
            Falloff = (Data.rayHit.distance + AdditionalDist).Map(Data.damageFalloff.x, Data.damageFalloff.y, 1f, BulletWeapon.s_falloffMin);
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
