using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.Properties.Effects.Triggers;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ArmorShred : 
        TriggerModDebuff,
        IGunProperty,
        IMeleeProperty
    {
        public ArmorShred() : base()
        {
            SetValidTriggers(DamageType.Player | DamageType.Lock, ITrigger.PositionalTriggers);
        }

        protected override DebuffModifierBase AddModifier(IDamageable damageable)
        {
            return DebuffManager.AddArmorShredDebuff(damageable, 1f, DebuffID);
        }
    }
}
