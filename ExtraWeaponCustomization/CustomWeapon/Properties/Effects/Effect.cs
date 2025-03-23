using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using System.Linq;
using EWC.JSON;
using EWC.Utils.Log;
using EWC.CustomWeapon.Enums;

namespace EWC.CustomWeapon.Properties.Effects
{
    public abstract class Effect : WeaponPropertyBase,
        ITriggerCallback
    {
        private TriggerCoordinator? _coordinator;
        public TriggerCoordinator? Trigger
        { 
            get => _coordinator;
            set
            {
                _coordinator = value;
                if (value != null)
                    value.Parent = this;
            }
        }

        protected virtual bool IsTriggerValid => Trigger?.Activate.Triggers.Any() == true;

        private TriggerName[]? _validTriggers;
        private DamageType _blacklistType = DamageType.Any;

        public override bool ShouldRegister(System.Type contextType)
        {
            if (Trigger == null && contextType == typeof(WeaponTriggerContext)) return false;
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponTriggerContext context) => Trigger!.Invoke(context);

        protected void SetValidTriggers(DamageType blacklist = DamageType.Any, params TriggerName[] names)
        {
            _validTriggers = names.Length > 0 ? names : null;
            _blacklistType = blacklist;
            VerifyTriggers();
        }
        protected void SetValidTriggers(params TriggerName[] names) => SetValidTriggers(DamageType.Any, names);

        public abstract void TriggerApply(List<TriggerContext> triggerList);
        public abstract void TriggerReset();

        public override WeaponPropertyBase Clone()
        {
            var copy = (Effect) base.Clone();
            copy.Trigger = Trigger?.Clone();
            return copy;
        }

        protected void SerializeTrigger(Utf8JsonWriter writer)
        {
            if (Trigger != null)
            {
                writer.WritePropertyName(nameof(Trigger));
                EWCJson.Serialize(writer, Trigger);
            }
            else
                writer.WriteString(nameof(Trigger), "Invalid");
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch(property)
            {
                case "triggertype":
                case "trigger": 
                    Trigger = TriggerCoordinator.Deserialize(ref reader, true);
                    VerifyTriggers();
                    break;
            }
        }

        private void VerifyTriggers()
        {
            if (Trigger == null) return;

            for (int i = Trigger.Activate.Triggers.Count - 1; i >= 0; i--)
            {
                TriggerName name = Trigger.Activate.Triggers[i].Name;
                // If the trigger isn't of the valid class, remove it
                if (_validTriggers != null && !_validTriggers.Contains(name))
                {
                    EWCLogger.Warning($"{GetType().Name} has an invalid trigger {name}. Only the following are allowed: {string.Join(", ", _validTriggers)}");
                    Trigger.Activate.Triggers.RemoveAt(i);
                    continue;
                }

                if (Trigger.Activate.Triggers[i] is not IDamageTypeTrigger typeTrigger) continue;

                typeTrigger.BlacklistType |= _blacklistType;
                // If all valid triggers are blacklisted, remove it
                if (typeTrigger.DamageTypes.All(type => type.HasAnyFlag(typeTrigger.BlacklistType)))
                {
                    EWCLogger.Warning($"{GetType().Name} has a trigger {name} with invalid damage types {string.Join(", ", typeTrigger.DamageTypes)}. It cannot contain any types within {_blacklistType}");
                    Trigger.Activate.Triggers.RemoveAt(i);
                    continue;
                }
            }

            if (!IsTriggerValid)
                Trigger = null;
        }
    }
}
