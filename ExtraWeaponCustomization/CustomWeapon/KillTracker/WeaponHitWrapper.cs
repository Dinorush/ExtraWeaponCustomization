using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.KillTracker
{
    public class WeaponHitWrapper
    {
        public BulletWeapon Weapon { get; }
        public DamageType DamageType { get; }

        public WeaponHitWrapper(BulletWeapon weapon, DamageType damageType)
        {
            Weapon = weapon;
            DamageType = damageType;
        }
    }
}
