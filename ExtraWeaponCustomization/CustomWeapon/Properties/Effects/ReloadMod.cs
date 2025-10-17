using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ReloadMod :
        TriggerMod,
        IWeaponProperty<WeaponReloadContext>
    {
        private readonly TriggerStack _triggerStack;

        protected override OwnerType RequiredOwnerType => OwnerType.Managed;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public ReloadMod() => _triggerStack = new(this);
        public override void TriggerReset() => _triggerStack.Clear();
        public override void TriggerApply(List<TriggerContext> contexts) => _triggerStack.Add(contexts);

        public void Invoke(WeaponReloadContext context)
        {
            if (_triggerStack.TryGetMod(out float mod))
                context.AddMod(mod, StackLayer);
        }
    }
}
