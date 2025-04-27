using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponShotEndContext : WeaponDamageTypeContext
    {
        public ShotInfo.Const? OldInfo { get; }

        public WeaponShotEndContext(DamageType baseType, ShotInfo.Const info, ShotInfo.Const? oldInfo) : base(baseType, info)
        {
            OldInfo = oldInfo;
        }

        public float DiffHits() => ShotInfo.Hits - (OldInfo?.Hits ?? 0);
        public float DiffTypeHits(DamageType[] damageTypes, DamageType blacklistType) => ShotInfo.TypeHits(damageTypes, blacklistType) - (OldInfo?.TypeHits(damageTypes, blacklistType) ?? 0);
    }
}
