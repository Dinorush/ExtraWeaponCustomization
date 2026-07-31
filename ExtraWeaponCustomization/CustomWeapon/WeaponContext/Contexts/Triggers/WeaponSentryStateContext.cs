using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredWeaponType: Enums.WeaponType.SentryHolder)]
    public sealed class WeaponSentryStateContext : WeaponTriggerContext
    {
        public bool Deployed { get; }
        public WeaponSentryStateContext(bool deployed) : base()
        {
            Deployed = deployed;
        }
    }
}
