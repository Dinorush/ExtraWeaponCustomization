using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.Properties.Effects.TriggerModifier;
using EWC.CustomWeapon.Properties.Shared.Triggers;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ArmorShred : 
        TriggerModDebuff
    {
        public ArmorShred() : base()
        {
            SetValidTriggers(DamageType.Player | DamageType.Object, ITrigger.HitTriggers);
        }

        protected override DebuffModifierBase AddModifier(IDamageable damageable)
        {
            return DebuffManager.AddArmorShredDebuff(damageable, 1f, StackLayer, DebuffID);
        }
    }
}
