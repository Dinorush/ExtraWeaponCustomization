using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Local)]
    public sealed class WeaponStaminaContext : WeaponTriggerContext
    {
        public float Stamina { get; }

        public WeaponStaminaContext(float stamina) : base()
        {
            Stamina = stamina;
        }
    }
}
