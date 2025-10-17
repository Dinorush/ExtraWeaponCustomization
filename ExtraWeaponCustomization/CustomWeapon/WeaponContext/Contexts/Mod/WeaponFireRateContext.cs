using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Any)]
    public sealed class WeaponFireRateContext : WeaponStackModContext
    {
        public WeaponFireRateContext(float fireRate) : base(fireRate) { }
    }
}
