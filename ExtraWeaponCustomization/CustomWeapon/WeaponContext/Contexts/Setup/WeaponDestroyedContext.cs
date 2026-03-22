using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Any)]
    public sealed class WeaponDestroyedContext : IWeaponContext
    {
        public WeaponDestroyedContext()
        {
        }
    }
}
