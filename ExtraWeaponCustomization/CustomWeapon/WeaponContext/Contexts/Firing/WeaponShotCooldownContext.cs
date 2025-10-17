using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Any, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponShotCooldownContext : IWeaponContext
    {
        public float NextShotTime { get; }

        public WeaponShotCooldownContext(float nextShotTime)
        {
            NextShotTime = nextShotTime;
        }
    }
}
