using Agents;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ShotModDebuff : 
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        ITriggerCallbackAgentSync
    {
        public ushort SyncID { get; set; }

        public StatType StatType { get; private set; } = StatType.Damage;
        public DamageType[] DamageType { get; private set; } = DamageTypeConst.Any;
        public uint DebuffID { get; private set; } = DebuffManager.DefaultGroup;

        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        private readonly Dictionary<BaseDamageableWrapper, (TriggerStack stack, DebuffModifierBase modifier)> _storedDebuffs = new();
        private readonly Dictionary<BaseDamageableWrapper, (TriggerStack stack, DebuffModifierBase modifier)> _activeDebuffs = new();
        private Coroutine? _updateRoutine;

        public ShotModDebuff()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.Hit));
            SetValidTriggers(ITrigger.PositionalTriggers);
        }

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
                    if (damageable == null || damageable.GetBaseAgent() == null) continue;

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
                if (damageable == null || damageable.GetBaseAgent() == null) return;

                AddTriggerInstance(
                    new BaseDamageableWrapper(damageable),
                    contexts[0].triggerAmt
                    );
                TriggerManager.SendInstance(this, damageable.GetBaseAgent());
            }
        }

        public void TriggerApplySync(Agent target, float mod)
        {
            Dam_SyncedDamageBase? damageable = target.Type switch
            {
                AgentType.Player => target.Cast<Player.PlayerAgent>().Damage,
                AgentType.Enemy => target.Cast<Enemies.EnemyAgent>().Damage,
                _ => null
            };

            if (damageable == null) return;

            AddTriggerInstance(new BaseDamageableWrapper(damageable.Cast<IDamageable>()), mod);
        }

        private void AddTriggerInstance(BaseDamageableWrapper wrapper, float triggerAmt)
        {
            if (!_storedDebuffs.TryGetValue(wrapper, out var debuff))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                _storedDebuffs.Keys
                    .Where(wrapper => !wrapper.Alive)
                    .ToList()
                    .ForEach(wrapper => {
                        _storedDebuffs[wrapper].modifier.Disable();
                        _storedDebuffs.Remove(wrapper);
                        _activeDebuffs.Remove(wrapper);
                    });

                debuff.stack = new(this);
                debuff.modifier = DebuffManager.AddShotModDebuff(wrapper.Object!, 1f, StatType, StackLayer, DamageType, DebuffID);
                _storedDebuffs[wrapper] = debuff;
            }

            _activeDebuffs[wrapper] = debuff;
            debuff.stack.Add(triggerAmt);

            if (debuff.stack.TryGetMod(out var mod))
            {
                debuff.modifier.Enable(mod);
                StartUpdate();
            }
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

            _updateRoutine = null;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteString(nameof(StatType), StatType.ToString());
            writer.WriteString(nameof(DamageType), DamageType[0].ToString());
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(CombineModifiers), CombineModifiers);
            writer.WriteNumber(nameof(CombineDecayTime), CombineDecayTime);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            writer.WriteNumber(nameof(DebuffID), DebuffID);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "modstattype":
                case "stattype":
                case "modstat":
                case "stat":
                    StatType = reader.GetString().ToEnum(StatType.Damage);
                    break;
                case "moddamagetype":
                case "damagetype":
                    DamageType = reader.GetString().ToDamageTypes();
                    break;
                case "debuffid":
                    if (reader.TokenType == JsonTokenType.String)
                        DebuffID = DebuffManager.StringIDToInt(reader.GetString()!);
                    else
                        DebuffID = reader.GetUInt32();
                    break;
            }
        }
    }
}
