using System;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    [Flags]
    public enum DamageFlag
    {
        Invalid = -1,
        Any = 0,
        Weakspot = 1,
        Bullet = 2, WeakspotBullet = Weakspot | Bullet,
        Explosive = 4, Explo = Explosive, WeakspotExplosive = Weakspot | Explo, WeakspotExplo = WeakspotExplosive,
        DOT = 8, WeakspotDOT = Weakspot | DOT
    }

    public static class DamageFlagMethods
    {
        public static bool HasFlag(this DamageFlag incoming, DamageFlag expected)
        {
            return incoming.HasFlag(expected);
        }
        public static DamageFlag ToDamageFlag(this string type)
        {
            return Enum.TryParse(type.Replace(" ", ""), true, out DamageFlag result) ? result : DamageFlag.Invalid;
        }
    }
}
