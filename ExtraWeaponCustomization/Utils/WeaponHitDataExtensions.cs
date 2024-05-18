using Gear;
using static Weapon;

namespace ExtraWeaponCustomization.Utils
{
    internal static class WeaponHitExtensions
    {
        public static float Falloff(this WeaponHitData data, float additionalDist = 0)
        {
            return (data.rayHit.distance + additionalDist).Map(data.damageFalloff.x, data.damageFalloff.y, 1f, BulletWeapon.s_falloffMin);
        }
    }
}
