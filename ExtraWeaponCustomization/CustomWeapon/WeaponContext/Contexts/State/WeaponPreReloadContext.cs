using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Local, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponPreReloadContext : IWeaponContext
    {
        public bool Allow { get; set; } = true;

        public WeaponPreReloadContext() { }
    }
}
