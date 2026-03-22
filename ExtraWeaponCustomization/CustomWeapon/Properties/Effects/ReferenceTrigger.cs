using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ReferenceTrigger :
        Effect,
        IReferenceHolder,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        public PropertyList Properties { get; private set; } = new();
        public bool ResetOnTrigger { get; private set; } = false;
        public bool SendToTrigger { get; private set; } = false;
        public bool SendReset { get; private set; } = true;
        public float LoopDuration { get; private set; } = 0f;
        public float LoopInterval { get; private set; } = 1f;
        public float LoopDelay { get; private set; } = 0f;
        public bool LoopAddNewTriggers { get; private set; } = false;

        private List<ITriggerCallback>? _callbackProperties;
        private float _endLoopTime = 0f;
        private float _nextLoopTime = 0f;
        private List<TriggerContext>? _loopContexts = null;
        private Coroutine? _loopRoutine = null;

        protected override bool IsTriggerValid => Trigger?.Activate.Triggers.Any() == true || Trigger?.Reset != null;

        public override bool ShouldRegister(Type contextType)
        {
            if (LoopDuration <= 0)
            {
                if (contextType == typeof(WeaponSetupContext)) return false;
                if (contextType == typeof(WeaponClearContext)) return false;
            }
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponSetupContext context)
        {
            if (Clock.Time < _endLoopTime)
                _loopRoutine = CoroutineManager.StartCoroutine(LoopUpdate(inProgress: true).WrapToIl2Cpp());
        }

        public void Invoke(WeaponClearContext context)
        {
            CoroutineUtil.Stop(ref _loopRoutine);
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (LoopDuration <= 0f)
                SendApply(contexts);
            else
            {
                if (_loopContexts == null)
                    _loopContexts = contexts;
                else if (LoopAddNewTriggers)
                    _loopContexts.AddRange(contexts);
                StartLoop();
            }
        }

        private void SendApply(List<TriggerContext> contexts)
        {
            foreach (var callback in _callbackProperties.OrEmptyIfNull())
            {
                if (!ResetOnTrigger)
                {
                    if (SendToTrigger && callback.Trigger != null)
                        callback.Trigger.RemoteActivateTrigger(contexts);
                    else
                        callback.TriggerApply(contexts);
                }
                else
                {
                    if (SendToTrigger && callback.Trigger != null)
                        callback.Trigger.RemoteResetTrigger(contexts);
                    else
                        callback.RemoteReset();
                }
            }
        }

        public override void TriggerReset()
        {
            CoroutineUtil.Stop(ref _loopRoutine);
            _loopContexts = null;
            if (SendReset)
                foreach (var callback in _callbackProperties.OrEmptyIfNull())
                    callback.RemoteReset();
        }

        private void StartLoop()
        {
            _endLoopTime = Clock.Time + LoopDuration;
            _loopRoutine ??= CoroutineManager.StartCoroutine(LoopUpdate().WrapToIl2Cpp());
        }

        private IEnumerator LoopUpdate(bool inProgress = false)
        {
            if (inProgress)
            {
                if (Clock.Time < _nextLoopTime)
                    yield return new WaitForSeconds(_nextLoopTime - Clock.Time);
            }
            else
            {
                _nextLoopTime = Clock.Time + LoopDelay;
                if (LoopDelay > 0f)
                    yield return new WaitForSeconds(LoopDelay);
            }

            while (Clock.Time < _endLoopTime)
            {
                SendApply(_loopContexts!);
                _nextLoopTime = Clock.Time + LoopInterval;
                yield return new WaitForSeconds(LoopInterval);
            }
            _loopRoutine = null;
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
            writer.WriteBoolean(nameof(ResetOnTrigger), ResetOnTrigger);
            writer.WriteBoolean(nameof(SendToTrigger), SendToTrigger);
            writer.WriteBoolean(nameof(SendReset), SendReset);
            writer.WriteNumber(nameof(LoopDuration), LoopDuration);
            writer.WriteNumber(nameof(LoopInterval), LoopInterval);
            writer.WriteNumber(nameof(LoopDelay), LoopDelay);
            writer.WriteBoolean(nameof(LoopAddNewTriggers), LoopAddNewTriggers);
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
                case "resetontrigger":
                    ResetOnTrigger = reader.GetBoolean();
                    break;
                case "sendapplyamount":
                case "sendamount":
                    SendToTrigger = reader.GetBoolean();
                    break;
                case "sendreset":
                    SendReset = reader.GetBoolean();
                    break;
                case "loopduration":
                case "duration":
                    LoopDuration = reader.GetSingle();
                    break;
                case "loopinterval":
                case "interval":
                    LoopInterval = reader.GetSingle();
                    break;
                case "loopdelay":
                case "delay":
                    LoopDelay = reader.GetSingle();
                    break;
                case "loopaddnewtriggers":
                case "addnewtriggers":
                    LoopAddNewTriggers = reader.GetBoolean();
                    break;
            }
        }
    }
}
