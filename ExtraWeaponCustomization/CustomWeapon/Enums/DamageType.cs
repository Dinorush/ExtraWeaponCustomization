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
        Shrapnel = 1<<5,
        Foam = 1<<6,
        Armor = 1<<7,
        Flesh = 1<<8,
        Foamed = 1<<9,
        Unfoamed = 1<<10,
        Staggered = 1<<11,
        Unstaggered = 1<<12,
        Enemy = 1<<13,
        Player = 1<<14,
        Lock = 1<<15,
        Dead = 1<<16,
        Terrain = 1<<17 | Dead
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

            if (name.Contains("unstaggered"))
                flag |= DamageType.Unstaggered;
            else if (name.Contains("staggered"))
                flag |= DamageType.Staggered;

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
            else if (name.Contains("shrapnel"))
                flag |= DamageType.Shrapnel;
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

        public static DamageType GetBaseType(this DamageType type) => type & (DamageType.Bullet | DamageType.Explosive | DamageType.DOT | DamageType.Shrapnel);
        public static DamageType GetBaseType(this DamageType[] types)
        {
            DamageType result = (DamageType)~0;
            foreach (var type in types)
                result &= GetBaseType(type);
            return result;
        }

        public static DamageType GetSubTypes(Dam_EnemyDamageLimb limb)
        {
            DamageType damageType = DamageType.Enemy;
            damageType |= limb.m_type == eLimbDamageType.Weakspot ? DamageType.Weakspot : DamageType.Body;
            damageType |= limb.m_armorDamageMulti < 1f ? DamageType.Armor : DamageType.Flesh;
            damageType |= limb.m_base.IsStuckInGlue ? DamageType.Foamed : DamageType.Unfoamed;

            var stateEnum = limb.m_base.Owner.Locomotion.CurrentStateEnum;
            damageType |= stateEnum == Enemies.ES_StateEnum.Hitreact || stateEnum == Enemies.ES_StateEnum.HitReactFlyer ? DamageType.Staggered : DamageType.Unstaggered;
            return damageType;
        }

        public static DamageType GetSubTypes(IDamageable damageable)
        {
            Agent? agent = damageable.GetBaseAgent();
            if (agent == null)
            {
                if (damageable.TryCast<LevelGeneration.LG_WeakLockDamage>() != null)
                    return DamageType.Flesh | DamageType.Body | DamageType.Unfoamed | DamageType.Lock;
                else
                    return DamageType.Terrain;
            }

            return agent.Type switch
            {
                AgentType.Enemy => damageable!.GetBaseDamagable().GetHealthRel() > 0 ? GetSubTypes(damageable.Cast<Dam_EnemyDamageLimb>()) : DamageType.Dead,
                AgentType.Player => DamageType.Flesh | DamageType.Body | DamageType.Unfoamed | DamageType.Unstaggered | DamageType.Player,
                _ => DamageType.Any
            };
        }

        public static DamageType WithSubTypes(this DamageType damageType, Dam_EnemyDamageLimb limb) => damageType | GetSubTypes(limb);

        public static DamageType WithSubTypes(this DamageType damageType, IDamageable damageable) => damageType | GetSubTypes(damageable);
    }
}
