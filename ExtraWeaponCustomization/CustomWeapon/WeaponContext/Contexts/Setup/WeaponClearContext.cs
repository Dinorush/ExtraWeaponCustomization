using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponClearContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponClearContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
