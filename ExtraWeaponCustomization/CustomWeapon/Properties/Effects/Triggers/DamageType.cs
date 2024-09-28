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
        DOT = 8, WeakspotDOT = Weakspot | DOT
    }
}
