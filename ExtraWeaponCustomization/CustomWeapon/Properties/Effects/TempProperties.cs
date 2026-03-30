using EWC.CustomWeapon.Properties.Shared.Triggers;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class TempProperties :
        Effect,
        ITriggerCallbackBasicSync,
        IPropertyHolder
    {
        public ushort SyncID { get; set; }

        public PropertyList Properties { get; private set; } = new();
        private List<ITriggerCallback>? _callbackProperties;
        public float Duration { get; private set; } = 0f;
        public bool Override { get; private set; } = false;
        public bool ResetTriggersOnEnd { get; private set; } = false;

        private PropertyNode _node = null!;
        public PropertyNode Node
        {
            get => _node;
            set
            {
                _node = value;
                _node.Override = Override;
            }
        }

        private readonly DelayedCallback _applyCallback;

        public TempProperties()
        {
            _applyCallback = new(
                () => Duration,
                ApplyProperties,
                RemoveProperties
            );
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            TriggerApplySync();
            TriggerManager.SendInstance(this);

            foreach (var callback in _callbackProperties.OrEmptyIfNull())
                if (callback.Trigger == null)
                    callback.TriggerApply(contexts);
        }

        public void TriggerApplySync(float mod = 1f) => CWC.StartDelayedCallback(_applyCallback);

        public override void TriggerReset()
        {
            TriggerResetSync();
            TriggerManager.SendReset(this);

            foreach (var callback in _callbackProperties.OrEmptyIfNull())
                if (callback.Trigger == null)
                    callback.TriggerReset();
        }

        public void TriggerResetSync()
        {
            _applyCallback.Stop();
        }

        private void ApplyProperties()
        {
            CWC.ActivateNode(Node);
        }

        private void RemoveProperties()
        {
            CWC.DeactivateNode(Node);

            if (ResetTriggersOnEnd && _callbackProperties != null)
            {
                foreach (var property in _callbackProperties)
                    property.RemoteReset();
            }
        }

        public override WeaponPropertyBase Clone()
        {
            var copy = (TempProperties) base.Clone();
            copy.Properties = Properties.Clone();
            return copy;
        }

        public void AddTriggerCallback(ITriggerCallback callback)
        {
            _callbackProperties ??= new();
            _callbackProperties.Add(callback);
        }

        public override void OnPropertiesSetup()
        {
            foreach (var property in Properties)
                if (property is ITriggerCallback callback)
                    AddTriggerCallback(callback);
            base.OnPropertiesSetup();
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            EWCJson.Serialize(writer, nameof(Properties), Properties);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(Override), Override);
            writer.WriteBoolean(nameof(ResetTriggersOnEnd), ResetTriggersOnEnd);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "properties":
                    Properties = EWCJson.Deserialize<PropertyList>(ref reader)!;
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
                case "override":
                    Override = reader.GetBoolean();
                    break;
                case "resettriggersonend":
                    ResetTriggersOnEnd = reader.GetBoolean();
                    break;
            }
        }
    }
}
