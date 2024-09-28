using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    // Used by CWCs in the event that client has incorrect Sync properties
    public sealed class TriggerCallbackSyncDummy : ITriggerCallbackSync
    {
        public static TriggerCallbackSyncDummy Instance = new();

        public ushort SyncID { get; set; }
#pragma warning disable CS8618 // CWC is never used by this
        public CustomWeaponComponent CWC { get; set; }
#pragma warning restore CS8618

        public TriggerCoordinator? Trigger { get; set; }

        public IWeaponProperty Clone() { return this; }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader) { }

        public void Invoke(WeaponTriggerContext context) { }

        public void Serialize(Utf8JsonWriter writer) { }

        public void TriggerApply(List<TriggerContext> triggerList) { }

        public void TriggerApplySync(float mod) { }

        public void TriggerReset() { }

        public void TriggerResetSync() { }
    }
}
