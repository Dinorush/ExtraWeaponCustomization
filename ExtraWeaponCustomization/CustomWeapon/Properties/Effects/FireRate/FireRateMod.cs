using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects
{
    public class FireRateMod :
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        ITriggerCallbackBasicSync,
        IWeaponProperty<WeaponFireRateContext>
    {
        public ushort SyncID { get; set; }

        private readonly TriggerStack _triggerStack;

        public FireRateMod() => _triggerStack = new(this);

        public override void TriggerReset()
        {
            _triggerStack.Clear();

            TriggerManager.SendReset(this);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
           _triggerStack.Add(contexts);

            TriggerManager.SendInstance(this, Sum(contexts));
        }

        public void TriggerResetSync()
        {
            _triggerStack.Clear();
        }

        public void TriggerApplySync(float num)
        {
            _triggerStack.Add(num);
        }

        public void Invoke(WeaponFireRateContext context)
        {
            if (_triggerStack.TryGetMod(out float mod))
                context.AddMod(mod, StackLayer);
        }
    }
}
