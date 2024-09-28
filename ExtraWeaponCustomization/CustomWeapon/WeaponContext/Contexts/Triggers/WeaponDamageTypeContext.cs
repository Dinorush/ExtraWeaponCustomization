using EWC.CustomWeapon.Properties.Effects.Triggers;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponDamageTypeContext : WeaponTriggerContext
    {
        public DamageType DamageType { get; protected set; }

        public WeaponDamageTypeContext(DamageType flag) : base()
        {
            DamageType = flag;
        }
    }
}
