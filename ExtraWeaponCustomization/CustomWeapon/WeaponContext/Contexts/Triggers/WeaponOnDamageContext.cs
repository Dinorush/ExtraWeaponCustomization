using ExtraWeaponCustomization.Utils;
using Gear;
using System;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    internal class WeaponOnDamageContext : WeaponTriggerContext
    {
        public float Damage { get; }
        public IDamageable Damageable { get; }
        public WeaponOnDamageContext(float damage, IDamageable damageable, BulletWeapon weapon, TriggerType type = TriggerType.OnDamage) : base(weapon, type)
        {
            Damage = damage;
            Damageable = damageable;
        }

        public WeaponOnDamageContext(WeaponHitData data, float additionalDist, Dam_EnemyDamageLimb limb, BulletWeapon weapon, TriggerType type = TriggerType.OnDamage) : base(weapon, type)
        {
            Damage = data.damage * data.Falloff(additionalDist);
            Damage = limb.ApplyWeakspotAndArmorModifiers(Damage, data.precisionMulti);
            Damage = limb.ApplyDamageFromBehindBonus(Damage, data.rayHit.point, data.fireDir.normalized);
            Damage = Math.Min(Damage, limb.m_base.HealthMax);
            if (limb.DestructionType == eLimbDestructionType.Custom)
                Damage = Math.Min(Damage, limb.m_healthMax);
            Damageable = limb.TryCast<IDamageable>()!;
        }
    }
}
