using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class SpreadMod :
        TriggerModTimed
    {
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        protected override void OnUpdate(float mod) => CGC.SpreadController.SetMod(this, mod);
        protected override void OnDisable() => CGC.SpreadController.ClearMod(this);
    }
}
