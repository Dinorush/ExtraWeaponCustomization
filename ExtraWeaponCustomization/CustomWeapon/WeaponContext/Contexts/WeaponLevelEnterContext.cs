using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponLevelEnterContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponLevelEnterContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
