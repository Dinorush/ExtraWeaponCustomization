using ExtraWeaponCustomization.Utils;
using Gear;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreHitContext : IWeaponContext
    {
        public WeaponHitData Data { get; }
        public float AdditionalDist { get; }
        public BulletWeapon Weapon { get; }
        public float Falloff { get; }

        public WeaponPreHitContext(ref WeaponHitData weaponHitData, float additionalDist, BulletWeapon weapon)
        {
            Data = weaponHitData;
            Weapon = weapon;
            AdditionalDist = additionalDist;
            Falloff = Data.Falloff(additionalDist);
        }
    }
}
