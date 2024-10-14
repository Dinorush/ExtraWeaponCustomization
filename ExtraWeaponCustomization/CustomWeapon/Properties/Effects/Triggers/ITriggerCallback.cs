using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public struct TriggerContext
    {
        public float triggerAmt;
        public IWeaponContext context;
    }

    public interface ITriggerCallback : IWeaponProperty<WeaponTriggerContext>
    {
        public TriggerCoordinator? Trigger { get; set; }
        public void TriggerApply(List<TriggerContext> triggerList);
        public void TriggerReset();
    }

    public interface ITriggerCallbackSync : ITriggerCallback, IWeaponProperty<WeaponTriggerContext>
    {
        public ushort SyncID { get; set; }

        public void TriggerApplySync(float mod);
        public void TriggerResetSync();
    }
}
