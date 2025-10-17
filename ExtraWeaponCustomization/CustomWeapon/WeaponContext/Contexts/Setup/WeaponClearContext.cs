using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Any)]
    public sealed class WeaponClearContext : IWeaponContext
    {
        public WeaponClearContext()
        {
        }
    }
}
