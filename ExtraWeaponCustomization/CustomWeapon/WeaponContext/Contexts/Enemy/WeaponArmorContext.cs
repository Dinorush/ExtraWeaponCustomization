using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponArmorContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public float ArmorMulti { get; set; }

        public WeaponArmorContext(float armor, BulletWeapon weapon)
        {
            ArmorMulti = armor;
            Weapon = weapon;
        }
    }
}
