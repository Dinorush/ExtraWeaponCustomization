using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.KillTracker
{
    public class WeaponHitWrapper
    {
        public BulletWeapon Weapon { get; }
        public DamageFlag DamageFlag { get; }

        public WeaponHitWrapper(BulletWeapon weapon, DamageFlag damageFlag)
        {
            Weapon = weapon;
            DamageFlag = damageFlag;
        }
    }
}
