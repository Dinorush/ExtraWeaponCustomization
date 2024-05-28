using System;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    [Flags]
    public enum TriggerType
    {
        Invalid = -1,
        OnFire = 1, OnShot = OnFire,
        OnHit = 2,
        OnHitBullet = OnHit | 4,
        OnHitExplo = OnHit | 8, OnHitExplosive = OnHitExplo,
        OnPrecHit = OnHit | 16, OnPrecisionHit = OnPrecHit,
        OnPrecHitBullet = OnPrecHit | OnHitBullet | 32, OnPrecisionHitBullet = OnPrecHitBullet,
        OnPrecHitExplo = OnPrecHit | OnHitExplo | 64, OnPrecHitExplosive = OnPrecHitExplo, OnPrecisionHitExplo = OnPrecHitExplo, OnPrecisionHitExplosive = OnPrecHitExplo,
        OnKill = 128,
        OnPrecKill = OnKill | 256, OnPrecisionKill = OnPrecKill,
        OnReload = 512,
        OnDamage = 1024,
        OnPrecDamage = OnDamage | 2048
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
