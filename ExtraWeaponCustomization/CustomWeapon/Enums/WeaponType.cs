using System;

namespace EWC.CustomWeapon.Enums
{
    [Flags]
    public enum WeaponType : byte
    {
        Any = 0,
        SentryHolder = 1,
        Melee = 1 << 1,
        Gun = 1 << 2,
        BulletWeapon = 1 << 3,
        Sentry = 1 << 4
    }
}
