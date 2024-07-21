using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using System.Collections.Generic;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public struct TriggerContext
    {
        public float triggerAmt;
        public IWeaponContext context;
    }

    public interface ITriggerCallback
    {
        public abstract void TriggerApply(List<TriggerContext> triggerList);
        public abstract void TriggerReset();
    }
}
