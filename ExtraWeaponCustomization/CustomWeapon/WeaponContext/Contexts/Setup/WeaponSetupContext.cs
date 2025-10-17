using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Any)]
    public sealed class WeaponSetupContext : IWeaponContext
    {
        public WeaponSetupContext()
        {
        }
    }
}
