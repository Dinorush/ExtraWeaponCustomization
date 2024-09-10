using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponTriggerContext : IWeaponContext
    {
        public BulletWeapon Weapon { get; }

        public WeaponTriggerContext(BulletWeapon weapon)
        {
            Weapon = weapon;
        }
    }
}
