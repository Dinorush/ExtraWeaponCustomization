using Agents;
using System;

namespace EWC.CustomWeapon.Enums
{
    [Flags]
    public enum DamageType
    {
        Invalid = -1,
        Any = 0,
        Body = 1,
        Weakspot = 1<<1,
        Bullet = 1<<2,
        Explosive = 1<<3,
        DOT = 1<<4,
        Foam = 1<<5,
        Armor = 1<<6,
        Flesh = 1<<7,
        Foamed = 1<<8,
        Unfoamed = 1<<9,
        Enemy = 1<<10,
        Player = 1<<11,
        Lock = 1<<12,
        Terrain = 1<<13
    }

    public static class DamageTypeConst
    {
        public static readonly DamageType[] Any = new[] { DamageType.Any };
    }

    public static class DamageTypeMethods
    {
        public static DamageType[] ToDamageTypes(this string? name)
        {
            if (name == null) return new[] { DamageType.Invalid };

            name = name.Replace(" ", null).ToLowerInvariant();
            string[] names = name.Split('|');

            DamageType[] types = new DamageType[names.Length];
            for (int i = 0; i < names.Length; i++)
                types[i] = Internal_ToDamageType(names[i]);

            return types;
        }

        public static DamageType ToDamageType(this string? name)
        {
            if (name == null) return DamageType.Invalid;

            name = name.Replace(" ", null).ToLowerInvariant();
            return Internal_ToDamageType(name);
        }

        private static DamageType Internal_ToDamageType(string name)
        {
            DamageType flag = DamageType.Any;
            if (name.Contains("prec") || name.Contains("weakspot"))
                flag |= DamageType.Weakspot;
            else if (name.Contains("body"))
                flag |= DamageType.Body;

            if (name.Contains("unfoamed"))
                flag |= DamageType.Unfoamed;
            else if (name.Contains("foamed"))
                flag |= DamageType.Foamed;

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
            else if (name.Contains("glue"))
                flag |= DamageType.Foam;

            return flag;
        }

        public static bool HasAnyFlag(this DamageType type, DamageType flagSet) => (type & flagSet) != 0;
        public static bool HasFlagIn(this DamageType type, DamageType[] flagSet)
        {
            foreach (var flag in flagSet)
                if (type.HasFlag(flag))
                    return true;
            return false;
        }

        public static DamageType GetSubTypes(Dam_EnemyDamageLimb limb) => DamageType.Enemy | GetSubTypes(!limb.IsDestroyed && limb.m_type == eLimbDamageType.Weakspot, limb.m_armorDamageMulti, limb.m_base.IsStuckInGlue);

        public static DamageType GetSubTypes(IDamageable damageable)
        {
            Agent? agent = damageable.GetBaseAgent();
            if (agent == null)
            {
                if (damageable.TryCast<LevelGeneration.LG_WeakLockDamage>() != null)
                    return DamageType.Flesh | DamageType.Body | DamageType.Unfoamed | DamageType.Lock;
                else
                    return DamageType.Any;
            }

            return agent.Type switch
            {
                AgentType.Enemy => GetSubTypes(damageable.Cast<Dam_EnemyDamageLimb>()),
                AgentType.Player => DamageType.Flesh | DamageType.Body | DamageType.Unfoamed | DamageType.Player,
                _ => DamageType.Any
            };
        }

        public static DamageType GetSubTypes(bool precHit, float armorMulti, bool inGlue = false)
        {
            DamageType damageType = DamageType.Any;
            damageType |= precHit ? DamageType.Weakspot : DamageType.Body;
            damageType |= armorMulti < 1f ? DamageType.Armor : DamageType.Flesh;
            damageType |= inGlue ? DamageType.Foamed : DamageType.Unfoamed;
            return damageType;
        }

        public static DamageType WithSubTypes(this DamageType damageType, Dam_EnemyDamageLimb limb) => damageType | GetSubTypes(limb);

        public static DamageType WithSubTypes(this DamageType damageType, IDamageable damageable) => damageType | GetSubTypes(damageable);

        public static DamageType WithSubTypes(this DamageType damageType, bool precHit, float armorMulti) => damageType | GetSubTypes(precHit, armorMulti);
    }
}
