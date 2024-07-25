using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostSetupContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponPostSetupContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
