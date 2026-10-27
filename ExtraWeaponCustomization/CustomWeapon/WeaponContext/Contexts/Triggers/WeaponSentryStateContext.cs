using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Managed, requiredWeaponType: Enums.WeaponType.SentryHolder)]
    public sealed class WeaponSentryStateContext : WeaponTriggerContext
    {
        public bool Deployed { get; }
        public WeaponSentryStateContext(bool deployed) : base()
        {
            Deployed = deployed;
        }
    }
}
