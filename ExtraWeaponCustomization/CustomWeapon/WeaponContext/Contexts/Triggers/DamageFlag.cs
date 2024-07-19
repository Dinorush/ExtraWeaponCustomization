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
}
