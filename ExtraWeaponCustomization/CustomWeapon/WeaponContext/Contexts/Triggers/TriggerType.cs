using System;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    [Flags]
    public enum TriggerType
    {
        Invalid = -1,
        OnFire = 0, OnShot = OnFire,
        OnHit = 1,
        OnHitBullet = OnHit | 2,
        OnHitExplo = OnHit | 4, OnHitExplosive = OnHitExplo,
        OnPrecHit = OnHit | 8, OnPrecisionHit = OnPrecHit,
        OnPrecHitBullet = OnPrecHit | OnHitBullet | 16, OnPrecisionHitBullet = OnPrecHitBullet,
        OnPrecHitExplo = OnPrecHit | OnHitExplo | 32, OnPrecHitExplosive = OnPrecHitExplo, OnPrecisionHitExplo = OnPrecHitExplo, OnPrecisionHitExplosive = OnPrecHitExplo,
        OnKill = 64,
        OnReload = 128
    }

    public static class TriggerTypeMethods
    {
        public static bool IsType(this TriggerType incoming, TriggerType expected)
        {
            return incoming.HasFlag(expected);
        }

        public static TriggerType ToTriggerType(this string type)
        {
            return Enum.TryParse(type.Replace(" ", ""), true, out TriggerType result) ? result : TriggerType.Invalid;
        }
    }
}
