using Gear;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostRayContext : IWeaponContext
    {
        public WeaponHitData Data { get; }
        public BulletWeapon Weapon { get; }

        public WeaponPostRayContext(WeaponHitData weaponHitData, BulletWeapon weapon)
        {
            Data = weaponHitData;
            Weapon = weapon;
        }
    }
}
