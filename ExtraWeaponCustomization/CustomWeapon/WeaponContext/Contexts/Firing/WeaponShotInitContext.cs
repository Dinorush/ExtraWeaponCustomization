using EWC.CustomWeapon.CustomShot;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponShotInitContext : IWeaponContext
    {
        public ShotInfoMod Mod { get; set; }

        public WeaponShotInitContext(ShotInfoMod mod)
        {
            Mod = mod;
        }
    }
}
