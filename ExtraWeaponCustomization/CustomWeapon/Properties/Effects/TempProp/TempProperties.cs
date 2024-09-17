﻿using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.TempProp;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.JSON;
using Player;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using UnityEngine;
using CollectionExtensions = BepInEx.Unity.IL2CPP.Utils.Collections.CollectionExtensions;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class TempProperties :
        Effect,
        IWeaponProperty<WeaponTempPropertiesContextSync>
    {
        public PropertyList? Properties { get; set; }
        private List<ITriggerCallback>? _callbackProperties;
        public bool OverrideBase { get; set; } = false;
        public bool ResetTriggersOnEnd { get; set; } = false;
        public float Duration { get; set; } = 0f;

        internal PropertyNode? Node { get; set; }

        private Coroutine? _activeRoutine;
        private float _endTime;

        public override void TriggerReset()
        {
            _endTime = 0;
            if (_activeRoutine != null)
            {
                CoroutineManager.StopCoroutine(_activeRoutine);
                RemoveProperties();
            }
            _activeRoutine = null;
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (Properties == null) return;

            _endTime = Clock.Time + Duration;
            _activeRoutine ??= CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(DeactivateAfterDelay()));

            TempPropertiesManager.SendInstance(CWC.Weapon.Owner.Owner, PlayerAmmoStorage.GetSlotFromAmmoType(CWC.Weapon.AmmoType));
        }

        public void Invoke(WeaponTempPropertiesContextSync context)
        {
            if (Properties == null) return;

            _endTime = Clock.Time + Duration;
            _activeRoutine ??= CoroutineManager.StartCoroutine(CollectionExtensions.WrapToIl2Cpp(DeactivateAfterDelay()));
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
            CWC.Activate(Node!);
        }

        private void RemoveProperties()
        {
            CWC.Deactivate(Node!);

            if (ResetTriggersOnEnd && _callbackProperties != null)
            {
                foreach (var property in _callbackProperties)
                    property.TriggerReset();
            }
        }

        public override IWeaponProperty Clone()
        {
            TempProperties copy = new()
            {
                Properties = Properties?.Clone(),
                Duration = Duration,
                OverrideBase = OverrideBase,
                ResetTriggersOnEnd = ResetTriggersOnEnd,
                Trigger = Trigger?.Clone()
            };

            if (copy.Properties != null)
            {
                copy.Properties.Owner = copy;
                copy.Properties.Override = OverrideBase;
                foreach (var property in copy.Properties.Properties)
                {
                    if (property is ITriggerCallback trigger)
                    {
                        copy._callbackProperties ??= new();
                        copy._callbackProperties.Add(trigger);
                    }
                }
            }

            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WritePropertyName(nameof(Properties));
            if (Properties != null)
                EWCJson.Serialize(writer, Properties);
            else
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
            }
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(OverrideBase), OverrideBase);
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
                    try
                    {
                        var properties = EWCJson.Deserialize<List<IWeaponProperty>>(ref reader);
                        if (properties != null)
                            Properties = new(properties, false);
                    }
                    catch (JsonException) {}
                    break;
                case "duration":
                    Duration = reader.GetSingle();
                    break;
                case "overridebase":
                    OverrideBase = reader.GetBoolean();
                    break;
                case "resettriggersonend":
                    ResetTriggersOnEnd = reader.GetBoolean();
                    break;
            }
        }
    }
}