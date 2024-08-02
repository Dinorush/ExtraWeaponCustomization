using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponFireRateModContextSync : IWeaponContext
    {
        public float Mod { get; }
        public BulletWeapon Weapon { get; }

        public WeaponFireRateModContextSync(float mod, BulletWeapon weapon)
        {
            Mod = mod;
            Weapon = weapon;
        }
    }
}
