using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.JSON;
using EWC.Utils;
using EWC.Utils.Extensions;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ModSyncTrigger : ITrigger
    {
        public TriggerName Name => Activate.Name;
        public ITrigger Activate { get; private set; }
        public uint ID { get; private set; } = 0u;
        public float Amount { get; private set; } = 1f;
        public float Cap { get; private set; } = 0f;
        public float Threshold { get; private set; } = 0f;
        public bool ApplyAboveThreshold { get; private set; } = false;
        public bool OverrideAmount { get; private set; } = false;

        private TriggerMod? _syncedMod;
        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;
        private readonly static TriggerName[] ValidActivates = ITrigger.HitTriggers.Remove(TriggerName.Empty).Extend(TriggerName.Kill);

        public ModSyncTrigger()
        {
            Activate = null!;
        }

        public ModSyncTrigger(ITrigger trigger, uint id = 0)
        {
            Activate = trigger;
            ID = id;
        }
        public ModSyncTrigger(ITrigger trigger, string id) : this(trigger) => ID = WeaponPropertyBase.StringIDToInt(id);

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (_syncedMod == null) return false;

            if (!Activate.Invoke(context, out var triggerAmt)) return false;
            if (triggerAmt == 0) return true;

            float stacks;
            if (context is WeaponHitDamageableContextBase damContext)
            {
                if (!_syncedMod.TryGetStacks(out stacks, TempWrapper.Set(damContext.Damageable)))
                    return false;
            }
            else if (!_syncedMod.TryGetStacks(out stacks))
                return false;

            stacks *= Amount;
            if (stacks < Threshold) return false;

            if (OverrideAmount)
                amount = Amount;
            else if (ApplyAboveThreshold)
                amount = stacks - Threshold;
            else
                amount = stacks;

            if (Cap > 0 && amount > Cap)
                amount = Cap;
            return true;
        }

        public void Reset() { }

        public void OnReferenceSet(CustomWeaponComponent cwc)
        {
            if (cwc.TryGetReference(ID, out var property))
            {
                if (property is TriggerMod mod)
                {
                    if (mod.IsPerTarget && ValidActivates.Contains(Name))
                        _syncedMod = mod;
                    else
                        EWCLogger.Error($"Cannot sync with {mod.GetType().Name} since trigger {Name} is not a hit trigger!");
                }
                else
                    EWCLogger.Error($"Cannot use {property.GetType().Name} as a synced mod!");
            }
        }

        public ITrigger Clone()
        {
            var copy = CopyUtil.Clone(this);
            copy.Activate = Activate.Clone();
            return copy;
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "trigger":
                case "activate":
                    Activate = EWCJson.Deserialize<ITrigger>(ref reader)!;
                    if (Activate == null)
                        throw new JsonException($"ModSynced needs a valid Activate trigger!");
                    break;
                case "referenceid":
                case "id":
                    if (reader.TokenType == JsonTokenType.String)
                        ID = WeaponPropertyBase.StringIDToInt(reader.GetString()!);
                    else
                        ID = reader.GetUInt32();
                    break;
                case "cap":
                    Cap = reader.GetSingle();
                    break;
                case "threshold":
                    Threshold = reader.GetSingle();
                    break;
                case "applyabovethreshold":
                    ApplyAboveThreshold = reader.GetBoolean();
                    break;
                case "overrideamount":
                case "applyoverrideamount":
                case "applyamount":
                    OverrideAmount = reader.GetBoolean();
                    return;
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
            }
        }
    }
}
