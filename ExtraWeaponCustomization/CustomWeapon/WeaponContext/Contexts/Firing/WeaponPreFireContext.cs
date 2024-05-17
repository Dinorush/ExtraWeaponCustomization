using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreFireContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public bool Allow { get; set; }

        public WeaponPreFireContext(BulletWeapon weapon)
        {
            Weapon = weapon;
            Allow = true;
        }
    }
}
