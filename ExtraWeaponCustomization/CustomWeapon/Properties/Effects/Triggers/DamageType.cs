using Agents;
using System;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    [Flags]
    public enum DamageType
    {
        Invalid = -1,
        Any = 0,
        Body = 1,
        Weakspot = 2,
        Bullet = 4,
        Explosive = 8,
        DOT = 16,
        Armor = 32,
        Flesh = 64,
        Enemy = 128,
        Player = 256,
        Lock = 512, PlayerLock = Player | Lock
    }

    public static class DamageTypeMethods
    {
        public static DamageType ToDamageType(this string? name)
        {
            if (name == null) return DamageType.Invalid;

            name = name.Replace(" ", null).ToLowerInvariant();
            DamageType flag = DamageType.Any;
            if (name.Contains("prec") || name.Contains("weakspot"))
                flag |= DamageType.Weakspot;
            else if (name.Contains("body"))
                flag |= DamageType.Body;

            if (name.Contains("armor"))
                flag |= DamageType.Armor;
            else if (name.Contains("flesh"))
                flag |= DamageType.Flesh;

            if (name.Contains("enemy"))
                flag |= DamageType.Enemy;
            else if (name.Contains("player") || name.Contains("friendly"))
                flag |= DamageType.Player;
            else if (name.Contains("lock"))
                flag |= DamageType.Lock;

            if (name.Contains("bullet") || name.Contains("melee"))
                flag |= DamageType.Bullet;
            else if (name.Contains("explo"))
                flag |= DamageType.Explosive;
            else if (name.Contains("dot"))
                flag |= DamageType.DOT;

            return flag;
        }

        public static bool HasAnyFlag(this DamageType type, DamageType flagSet) => (type & flagSet) != 0;

        public static DamageType GetSubTypes(Dam_EnemyDamageLimb limb) => DamageType.Enemy | GetSubTypes(!limb.IsDestroyed && limb.m_type == eLimbDamageType.Weakspot, limb.m_armorDamageMulti);

        public static DamageType GetSubTypes(IDamageable damageable)
        {
            Agent? agent = damageable.GetBaseAgent();
            if (agent == null) return GetSubTypes(false, 1f) | DamageType.Lock;

            switch (agent.Type)
            {
                case AgentType.Enemy:
                    return GetSubTypes(damageable.Cast<Dam_EnemyDamageLimb>());
                case AgentType.Player:
                    return DamageType.Flesh | DamageType.Player;
                default:
                    return DamageType.Any;
            }
        }

        public static DamageType GetSubTypes(bool precHit, float armorMulti)
        {
            DamageType damageType = DamageType.Any;
            if (precHit) damageType |= DamageType.Weakspot;
            damageType |= armorMulti < 1f ? DamageType.Armor : DamageType.Flesh;
            return damageType;
        }

        public static DamageType WithSubTypes(this DamageType damageType, Dam_EnemyDamageLimb limb) => damageType | GetSubTypes(limb);

        public static DamageType WithSubTypes(this DamageType damageType, IDamageable damageable) => damageType | GetSubTypes(damageable);

        public static DamageType WithSubTypes(this DamageType damageType, bool precHit, float armorMulti) => damageType | GetSubTypes(precHit, armorMulti);
    }
}
