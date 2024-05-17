using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPreStartFireContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public bool Allow { get; set; }

        public WeaponPreStartFireContext(BulletWeapon weapon)
        {
            Weapon = weapon;
            Allow = true;
        }
    }
}
