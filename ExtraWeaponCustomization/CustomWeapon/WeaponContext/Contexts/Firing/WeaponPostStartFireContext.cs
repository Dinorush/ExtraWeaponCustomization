using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostStartFireContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponPostStartFireContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
