using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponCancelFireContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponCancelFireContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
