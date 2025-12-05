using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class RecoilMod :
        TriggerMod,
        IWeaponProperty<WeaponRecoilContext>
    {
        private readonly TriggerStack _triggerStack;

        protected override OwnerType RequiredOwnerType => OwnerType.Local;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public RecoilMod() => _triggerStack = new(this);
        public override void TriggerReset() => _triggerStack.Clear();
        public override void TriggerApply(List<TriggerContext> contexts) => _triggerStack.Add(contexts);

        public override bool TryGetStacks(out float stacks, BaseDamageableWrapper? _ = null) => _triggerStack.TryGetStacks(out stacks);

        public void Invoke(WeaponRecoilContext context)
        {
            if (_triggerStack.TryGetMod(out float mod))
                context.AddMod(mod, StackLayer);
        }
    }
}
