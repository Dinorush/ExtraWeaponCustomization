using System;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    [Flags]
    public enum DamageType
    {
        Invalid = -1,
        Any = 0,
        Weakspot = 1,
        Bullet = 2, WeakspotBullet = Weakspot | Bullet,
        Explosive = 4, WeakspotExplosive = Weakspot | Explosive,
        DOT = 8, WeakspotDOT = Weakspot | DOT,
        Armor = 16,
        Flesh = 32
    }

    public static class DamageTypeMethods
    {
        public static DamageType GetSubTypes(Dam_EnemyDamageLimb limb)
        {
            return GetSubTypes(limb.m_type == eLimbDamageType.Weakspot, limb.m_armorDamageMulti);
        }

        public static DamageType GetSubTypes(bool precHit, float armorMulti)
        {
            DamageType damageType = DamageType.Any;
            if (precHit) damageType |= DamageType.Weakspot;
            damageType |= armorMulti < 1f ? DamageType.Armor : DamageType.Flesh;
            return damageType;
        }

        public static DamageType WithSubTypes(this DamageType damageType, Dam_EnemyDamageLimb limb) => damageType | GetSubTypes(limb);

        public static DamageType WithSubTypes(this DamageType damageType, bool precHit, float armorMulti) => damageType | GetSubTypes(precHit, armorMulti);
    }
}
