using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.KillTracker
{
    public class WeaponHitWrapper
    {
        public BulletWeapon Weapon { get; }
        public bool PrecHit { get; }

        public WeaponHitWrapper(BulletWeapon weapon, bool precHit)
        {
            Weapon = weapon;
            PrecHit = precHit;
        }
    }
}
