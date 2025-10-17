using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    // Only used by auto-aim currently, avoiding costs for other things
    [RequireType(Enums.OwnerType.Local, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponUpdateContext : IWeaponContext
    {
        public WeaponUpdateContext() { }
    }
}
