using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Local)]
    public sealed class WeaponPreSprintContext : IWeaponContext
    {
        public bool AllowBurstCancel { get; set; } = false;
        public bool Allow { get; set; } = true;

        public WeaponPreSprintContext() {}
    }
}
