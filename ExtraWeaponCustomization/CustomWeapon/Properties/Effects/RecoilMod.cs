using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class RecoilMod :
        TriggerMod,
        IGunProperty,
        IWeaponProperty<WeaponRecoilContext>
    {
        private readonly TriggerStack _triggerStack;

        public RecoilMod() => _triggerStack = new(this);
        public override void TriggerReset() => _triggerStack.Clear();
        public override void TriggerApply(List<TriggerContext> contexts) => _triggerStack.Add(contexts);

        public void Invoke(WeaponRecoilContext context)
        {
            if (_triggerStack.TryGetMod(out float mod))
                context.AddMod(mod, StackLayer);
        }
    }
}
