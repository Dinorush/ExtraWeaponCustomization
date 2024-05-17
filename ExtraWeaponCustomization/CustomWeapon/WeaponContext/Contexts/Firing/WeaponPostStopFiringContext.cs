using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostStopFiringContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponPostStopFiringContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
