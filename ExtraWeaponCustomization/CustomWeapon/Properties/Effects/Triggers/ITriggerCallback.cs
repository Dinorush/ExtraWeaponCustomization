using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public struct TriggerContext
    {
        public float triggerAmt;
        public IWeaponContext context;
    }

    public interface ITriggerCallback : IWeaponProperty<WeaponTriggerContext>
    {
        public abstract void TriggerApply(List<TriggerContext> triggerList);
        public abstract void TriggerReset();
    }
}
