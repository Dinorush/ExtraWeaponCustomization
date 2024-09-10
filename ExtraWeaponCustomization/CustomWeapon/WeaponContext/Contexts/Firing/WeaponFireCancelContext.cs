using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponFireCancelContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }
        public bool Allow { get; set; }

        public WeaponFireCancelContext(BulletWeapon weapon)
        {
            Weapon = weapon;
            Allow = true;
        }
    }
}
