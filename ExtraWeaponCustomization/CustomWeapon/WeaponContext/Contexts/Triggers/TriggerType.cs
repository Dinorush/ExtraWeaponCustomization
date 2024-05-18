using System;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public enum TriggerType
    {
        Invalid = -1,
        OnFire, OnShot = OnFire,
        OnHit,
        OnHitBullet,
        OnHitExplo, OnHitExplosion = OnHitExplo,
        OnKill,
        OnReload
    }

    public static class TriggerTypeMethods
    {
        public static bool IsType(this TriggerType incoming, TriggerType expected)
        {
            if (expected == TriggerType.OnHit)
                return incoming == expected || incoming == TriggerType.OnHitBullet || incoming == TriggerType.OnHitExplo;
            return expected == incoming;
        }

        public static TriggerType ToTriggerType(this string type)
        {
            return Enum.TryParse(type.Replace(" ", ""), true, out TriggerType result) ? result : TriggerType.Invalid;
        }
    }
}
