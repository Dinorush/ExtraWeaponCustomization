using Agents;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    // Used by CWCs in the event that client has incorrect Sync properties
    public sealed class TriggerCallbackSyncDummy : WeaponPropertyBase,
        ITriggerCallbackBasicSync,
        ITriggerCallbackDirSync,
        ITriggerCallbackAgentSync
    {
        public readonly static TriggerCallbackSyncDummy Instance = new();

        public ushort SyncID { get; set; }

        public TriggerCoordinator? Trigger { get; set; }

        public override WeaponPropertyBase Clone() { return this; }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader) { }

        public void Invoke(WeaponTriggerContext context) { }

        public override void Serialize(Utf8JsonWriter writer) { }

        public void TriggerApply(List<TriggerContext> triggerList) { }

        public void TriggerApplySync(Vector3 position, Vector3 dir, float mod) { }

        public void TriggerApplySync(Agent target, float mod) { }

        public void TriggerApplySync(float mod) { }

        public void TriggerReset() { }

        public void TriggerResetSync() { }
    }
}
