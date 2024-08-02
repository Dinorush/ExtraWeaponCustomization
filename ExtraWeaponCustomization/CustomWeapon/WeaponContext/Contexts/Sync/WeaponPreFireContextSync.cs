using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreFireContextSync : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponPreFireContextSync(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
