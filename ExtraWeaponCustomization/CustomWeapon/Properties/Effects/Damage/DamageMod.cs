using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class DamageMod :
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponDamageContext>
    {
        private readonly TriggerStack _triggerStack;

        public DamageMod() => _triggerStack = new(this);
        public override void TriggerReset() => _triggerStack.Clear();
        public override void TriggerApply(List<TriggerContext> contexts) => _triggerStack.Add(contexts);

        public void Invoke(WeaponDamageContext context)
        {
            if (_triggerStack.TryGetMod(out float mod))
                context.Damage.AddMod(mod, StackLayer);
        }
    }
}
