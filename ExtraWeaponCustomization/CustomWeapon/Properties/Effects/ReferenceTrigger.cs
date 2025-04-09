using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.JSON;
using EWC.Utils.Extensions;
using EWC.Utils.Log;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ReferenceTrigger :
        Effect,
        IGunProperty,
        IMeleeProperty,
        IReferenceHolder
    {
        public PropertyList Properties { get; private set; } = new();
        private List<ITriggerCallback>? _callbackProperties;

        protected override bool IsTriggerValid => Trigger?.Activate.Triggers.Any() == true || Trigger?.Apply != null || Trigger?.Reset != null;

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            foreach (var callback in _callbackProperties.OrEmptyIfNull())
                callback.TriggerApply(contexts);
        }

        public override void TriggerReset()
        {
            foreach (var callback in _callbackProperties.OrEmptyIfNull())
                callback.RemoteReset();
        }

        private void AddTriggerCallback(ITriggerCallback callback)
        {
            _callbackProperties ??= new();
            _callbackProperties.Add(callback);
        }

        public void OnReferenceSet(WeaponPropertyBase property)
        {
            if (property is ITriggerCallback callback)
                AddTriggerCallback(callback);
            else
                EWCLogger.Warning(nameof(ReferenceTrigger) + " contains a non-trigger property: " + property.GetType().Name);
        }

        public override WeaponPropertyBase Clone()
        {
            var copy = (ReferenceTrigger)base.Clone();
            copy.Properties = Properties.Clone();
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            EWCJson.Serialize(writer, nameof(Properties), Properties);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "references":
                case "properties":
                    Properties = EWCJson.Deserialize<PropertyList>(ref reader)!;
                    var list = Properties.Properties;
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i] is not ReferenceProperty)
                        {
                            EWCLogger.Warning(nameof(ReferenceTrigger) + " contains a non-reference property, removing: " + list[i].GetType().Name);
                            list.RemoveAt(i);
                        }
                    }
                    break;
            }
        }
    }
}
