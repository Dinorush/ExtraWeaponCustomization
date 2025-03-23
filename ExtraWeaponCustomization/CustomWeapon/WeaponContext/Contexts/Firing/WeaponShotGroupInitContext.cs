using EWC.CustomWeapon.CustomShot;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponShotGroupInitContext : IWeaponContext
    {
        public ShotInfoMod GroupMod { get; set; }

        public WeaponShotGroupInitContext(ShotInfoMod mod)
        {
            GroupMod = mod;
        }
    }
}
