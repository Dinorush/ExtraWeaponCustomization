using Agents;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.Attributes;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public abstract class TriggerModDebuff :
        TriggerMod,
        ITriggerCallbackAgentSync
    {
        public ushort SyncID { get; set; }

        public uint DebuffID { get; private set; } = DebuffManager.DefaultGroup;
        public uint GlobalID { get; private set; } = 0u;

        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        private readonly Dictionary<BaseDamageableWrapper, (TriggerStack stack, DebuffModifierBase modifier)> _storedDebuffs = new();
        private readonly Dictionary<BaseDamageableWrapper, (TriggerStack stack, DebuffModifierBase modifier)> _activeDebuffs = new();
        private readonly static Dictionary<uint, Dictionary<BaseDamageableWrapper, (TriggerStack, DebuffModifierBase modifier)>> s_globalStacks = new();
        private Coroutine? _updateRoutine;

        public TriggerModDebuff()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.Hit));
            SetValidTriggers(ITrigger.HitTriggers);
        }

        [InvokeOnCleanup]
        private static void ClearGlobal()
        {
            s_globalStacks.Clear();
        }

        public override bool IsPerTarget => true;

        public override bool TryGetStacks(out float stacks, BaseDamageableWrapper? damageable = null)
        {
            if (damageable == null)
            {
                stacks = 0f;
                return false;
            }

            if (!_activeDebuffs.TryGetValue(damageable, out var debuff))
            {
                stacks = 0f;
                return false;
            }

            return debuff.stack.TryGetStacks(out stacks);
        }

        protected abstract DebuffModifierBase AddModifier(IDamageable damageable);

        public override void TriggerReset() => TriggerResetSync();

        public void TriggerResetSync()
        {
            foreach (var (stack, modifier) in _storedDebuffs.Values)
            {
                modifier.Disable();
                stack.Clear();
            }

            CoroutineUtil.Stop(ref _updateRoutine);
            _activeDebuffs.Clear();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (contexts.Count > 1)
            {
                Dictionary<BaseDamageableWrapper, float> triggerDict = new();
                foreach (var context in contexts)
                {
                    var hitContext = (WeaponHitDamageableContextBase)context.context;

                    IDamageable damageable = hitContext.Damageable;
                    if (damageable == null) continue;

                    TempWrapper.Set(damageable);
                    if (!triggerDict.ContainsKey(TempWrapper))
                        triggerDict.Add(new BaseDamageableWrapper(TempWrapper), 0);

                    triggerDict[TempWrapper] += context.triggerAmt;
                }

                foreach ((BaseDamageableWrapper wrapper, float triggerAmt) in triggerDict)
                {
                    AddTriggerInstance(wrapper, triggerAmt);
                    TriggerManager.SendInstance(this, wrapper.Object!.GetBaseAgent());
                }
            }
            else
            {
                var hitContext = (WeaponHitDamageableContextBase)contexts[0].context;
                IDamageable damageable = hitContext.Damageable;
                if (damageable == null) return;

                AddTriggerInstance(
                    new BaseDamageableWrapper(damageable),
                    contexts[0].triggerAmt
                    );
                TriggerManager.SendInstance(this, damageable.GetBaseAgent());
            }
        }

        public void TriggerApplySync(Agent target, float mod)
        {
            AddTriggerInstance(new BaseDamageableWrapper(target.GetComponent<IDamageable>()), mod);
        }

        private void AddTriggerInstance(BaseDamageableWrapper wrapper, float triggerAmt)
        {
            if (!_storedDebuffs.TryGetValue(wrapper, out var debuff))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                _storedDebuffs.Keys
                    .Where(wrapper => !wrapper.Alive)
                    .ToList()
                    .ForEach(wrapper =>
                    {
                        _storedDebuffs[wrapper].modifier.Disable();
                        _storedDebuffs.Remove(wrapper);
                        _activeDebuffs.Remove(wrapper);
                    });

                _storedDebuffs.Add(wrapper, debuff = GetNewDebuff(wrapper));
            }

            _activeDebuffs[wrapper] = debuff;
            debuff.stack.Add(triggerAmt);

            if (debuff.stack.TryGetMod(out var mod))
            {
                debuff.modifier.Enable(mod);
                StartUpdate();
            }
        }

        private (TriggerStack stack, DebuffModifierBase modifier) GetNewDebuff(BaseDamageableWrapper wrapper)
        {
            if (GlobalID == 0)
            {
                return (new((TriggerMod)Clone()), AddModifier(wrapper.Object!));
            }

            if (!s_globalStacks.TryGetValue(GlobalID, out var debuffDict))
                s_globalStacks.Add(GlobalID, debuffDict = new());

            if (!debuffDict.TryGetValue(wrapper, out (TriggerStack stack, DebuffModifierBase mod) debuff))
            {
                debuffDict.Keys
                    .Where(wrapper => !wrapper.Alive)
                    .ToList()
                    .ForEach(wrapper =>
                    {
                        debuffDict.Remove(wrapper);
                    });

                debuffDict.Add(wrapper, debuff = (new((TriggerMod) Clone()), AddModifier(wrapper.Object!)));
            }

            return debuff;
        }

        private void StartUpdate()
        {
            _updateRoutine ??= CoroutineManager.StartCoroutine(DelayedUpdate().WrapToIl2Cpp());
        }

        private IEnumerator DelayedUpdate()
        {
            while (_activeDebuffs.Count > 0)
            {
                foreach ((var wrapper, (var triggerStack, var modifier)) in _activeDebuffs.ToArray())
                {
                    if (triggerStack.TryGetMod(out float mod))
                    {
                        modifier.Mod = mod;
                    }
                    else
                    {
                        modifier.Disable();
                        _activeDebuffs.Remove(wrapper);
                    }
                }
                yield return null;
            }

            _activeDebuffs.Clear();
            _updateRoutine = null;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(CombineModifiers), CombineModifiers);
            writer.WriteNumber(nameof(CombineDecayTime), CombineDecayTime);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteNumber(nameof(DebuffID), DebuffID);
            writer.WriteNumber(nameof(GlobalID), GlobalID);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "debuffids":
                    if (reader.TokenType == JsonTokenType.String)
                        DebuffID = DebuffManager.StringIDToInt(reader.GetString()!);
                    else
                        DebuffID = reader.GetUInt32();
                    break;
                case "globalid":
                    if (reader.TokenType == JsonTokenType.String)
                        GlobalID = DebuffManager.StringIDToInt(reader.GetString()!);
                    else
                        GlobalID = reader.GetUInt32();
                    break;
            }
        }
    }
}