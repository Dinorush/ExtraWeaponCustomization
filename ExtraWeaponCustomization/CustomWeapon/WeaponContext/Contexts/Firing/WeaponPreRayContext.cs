using Gear;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreRayContext : IWeaponContext
    {
        public WeaponHitData Data { get; }
        public BulletWeapon Weapon { get; }

        public WeaponPreRayContext(WeaponHitData weaponHitData, BulletWeapon weapon)
        {
            Data = weaponHitData;
            Weapon = weapon;
        }
    }
}
