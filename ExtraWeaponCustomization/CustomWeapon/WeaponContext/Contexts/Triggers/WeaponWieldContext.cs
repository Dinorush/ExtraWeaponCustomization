using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Player | Enums.OwnerType.Managed)]
    public sealed class WeaponWieldContext : WeaponTriggerContext
    {
        public WeaponWieldContext() : base() { }
    }
}
