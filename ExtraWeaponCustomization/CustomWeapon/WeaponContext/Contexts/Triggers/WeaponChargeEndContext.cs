using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Local)]
    public sealed class WeaponChargeEndContext : WeaponTriggerContext
    {
        public float Charge { get; }
        public WeaponChargeEndContext(float charge) : base()
        {
            Charge = charge;
        }
    }
}
