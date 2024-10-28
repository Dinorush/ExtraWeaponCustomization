﻿using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.JSON;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class TempProperties :
        Effect,
        IGunProperty,
        IMeleeProperty,
        ITriggerCallbackSync
    {
        public ushort SyncID { get; set; }

        public PropertyList Properties { get; private set; } = new();
        private List<ITriggerCallback>? _callbackProperties;
        public float Duration { get; private set; } = 0f;
        public bool Override { get; private set; } = false;
        public bool ResetTriggersOnEnd { get; private set; } = false;

        internal PropertyNode? Node { get; set; }

        private Coroutine? _activeRoutine;
        private float _endTime;

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            TriggerApplySync();
            TriggerManager.SendInstance(this);
            
            if (_callbackProperties != null)
                foreach (var callback in _callbackProperties)
                    if (callback.Trigger == null)
                        callback.TriggerApply(contexts);
        }

        public void TriggerApplySync(float mod = 1f)
        {
            _endTime = Clock.Time + Duration;
            _activeRoutine ??= CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(DeactivateAfterDelay()));
        }

        public override void TriggerReset()
        {
            TriggerResetSync();
            TriggerManager.SendReset(this);

            if (_callbackProperties != null)
                foreach (var callback in _callbackProperties)
                    if (callback.Trigger == null)
                        callback.TriggerReset();
        }

        public void TriggerResetSync()
        {
            _endTime = 0;
            if (_activeRoutine != null)
            {
                CoroutineManager.StopCoroutine(_activeRoutine);
                RemoveProperties();
            }
            _activeRoutine = null;
        }

        private IEnumerator DeactivateAfterDelay()
        {
            ApplyProperties();
            while (Clock.Time < _endTime)
                yield return new WaitForSeconds(_endTime - Clock.Time);
            RemoveProperties();
            _activeRoutine = null;
        }

        private void ApplyProperties()
        {
            CWC.ActivateNode(Node!);
        }

        private void RemoveProperties()
        {
            CWC.DeactivateNode(Node!);

            if (ResetTriggersOnEnd && _callbackProperties != null)
            {
                foreach (var property in _callbackProperties)
                    property.TriggerReset();
            }
        }

        public override WeaponPropertyBase Clone()
        {
            var copy = (TempProperties) base.Clone();

            copy.Properties = Properties.Clone();
            copy.Properties.Owner = copy;
            copy.Properties.Override = Override;
            foreach (var property in copy.Properties.Properties)
            {
                if (property is ITriggerCallback trigger)
                {
                    copy._callbackProperties ??= new();
                    copy._callbackProperties.Add(trigger);
                }
            }

            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WritePropertyName(nameof(Properties));
            EWCJson.Serialize(writer, Properties);
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
