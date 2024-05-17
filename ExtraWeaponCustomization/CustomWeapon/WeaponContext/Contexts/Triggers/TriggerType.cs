using System;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public enum TriggerType
    {
        Invalid = -1,
        OnFire,
        OnShot = OnFire,
        OnHit,
        OnKill,
        OnReload
    }

    public static class TriggerTypeMethods
    {
        public static TriggerType ToTriggerType(this string type)
        {
            return Enum.TryParse(type, true, out TriggerType result) ? result : TriggerType.Invalid;
        }
    }
}
