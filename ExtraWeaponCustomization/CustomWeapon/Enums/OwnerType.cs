using System;

namespace EWC.CustomWeapon.Enums
{
    [Flags]
    public enum OwnerType : byte
    {
        Any = 0,
        Managed = 1,
        Local = 1 << 1,
        Player = 1 << 2,
        Sentry = 1 << 3,
        Unmanaged = 1 << 4
    }
}
