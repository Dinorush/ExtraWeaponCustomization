using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostFireContextSync : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponPostFireContextSync(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
