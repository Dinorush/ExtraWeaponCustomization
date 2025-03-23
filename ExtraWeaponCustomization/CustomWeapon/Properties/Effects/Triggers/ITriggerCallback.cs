using Agents;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using UnityEngine;

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
        public void RemoteReset()
        {
            if (Trigger != null)
                Trigger.ForceReset();
            else
                TriggerReset();
        }
    }

    public interface ITriggerCallbackSync : ITriggerCallback, IWeaponProperty<WeaponTriggerContext>
    {
        public ushort SyncID { get; set; }

        public void TriggerResetSync();
    }

    public interface ITriggerCallbackBasicSync : ITriggerCallbackSync
    {
        public void TriggerApplySync(float mod);
    }

    public interface ITriggerCallbackDirSync : ITriggerCallbackSync
    {
        public void TriggerApplySync(Vector3 position, Vector3 dir, float mod);
    }

    public interface ITriggerCallbackAgentSync : ITriggerCallbackSync
    {
        public void TriggerApplySync(Agent target, float mod);
    }
}
