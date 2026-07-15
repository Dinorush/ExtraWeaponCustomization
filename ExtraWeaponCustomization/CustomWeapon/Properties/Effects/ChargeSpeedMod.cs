using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.TriggerModifier;
using EWC.CustomWeapon.Properties.Shared.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects
{
    public class ChargeSpeedMod :
        TriggerMod,
        IWeaponProperty<WeaponChargeSpeedContext>
    {

        private readonly TriggerStack _triggerStack;

        public ChargeSpeedMod() => _triggerStack = new(this);

        protected override WeaponType RequiredWeaponType => WeaponType.BulletWeapon;
        protected override OwnerType RequiredOwnerType => OwnerType.Local;

        public override bool TryGetStacks(out float stacks, BaseDamageableWrapper? _ = null) => _triggerStack.TryGetStacks(out stacks);

        public override void TriggerReset()
        {
            _triggerStack.Clear();

            CGC.UpdateChargeTime();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            var num = Count(contexts);
            _triggerStack.Add(num);

            CGC.UpdateChargeTime();
        }

        public void Invoke(WeaponChargeSpeedContext context)
        {
            if (_triggerStack.TryGetMod(out float mod))
                context.AddMod(mod, StackLayer);
        }
    }
}
