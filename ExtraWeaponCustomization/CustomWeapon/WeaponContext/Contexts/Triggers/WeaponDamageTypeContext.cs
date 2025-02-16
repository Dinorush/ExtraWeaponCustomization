using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponDamageTypeContext : WeaponTriggerContext
    {
        public DamageType DamageType { get; protected set; }
        public ShotInfo.Const ShotInfo { get; }

        public WeaponDamageTypeContext(DamageType flag, ShotInfo.Const info) : base()
        {
            DamageType = flag;
            ShotInfo = info;
        }
    }
}
