using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using System.Linq;
using EWC.JSON;
using EWC.Utils.Log;

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

        private TriggerName[]? _validTriggers;
        private DamageType _blacklistType = DamageType.Invalid;

        public void Invoke(WeaponTriggerContext context) => Trigger?.Invoke(context);

        protected void SetValidTriggers(DamageType flag = DamageType.Invalid, params TriggerName[] names)
        {
            _validTriggers = names;
            _blacklistType = flag;
            VerifyTriggers();
        }
        protected void SetValidTriggers(params TriggerName[] names) => SetValidTriggers(DamageType.Invalid, names);

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
            switch(property)
            {
                case "triggertype":
                case "trigger":
                    Trigger = EWCJson.Deserialize<TriggerCoordinator>(ref reader);
                    VerifyTriggers();
                    break;
            }
        }

        private void VerifyTriggers()
        {
            if (Trigger == null) return;

            for (int i = Trigger.Activate.Count - 1; i >= 0; i--)
            {
                TriggerName name = Trigger.Activate[i].Name;
                // If the trigger isn't of the valid class, remove it
                if (_validTriggers != null && !_validTriggers.Contains(name))
                {
                    EWCLogger.Warning(GetType().Name + " only allows triggers of the following types or subtypes: " + string.Join(", ", _validTriggers));
                    Trigger.Activate.RemoveAt(i);
                    continue;
                }

                if (Trigger.Activate[i] is not IDamageTypeTrigger typeTrigger) continue;

                typeTrigger.BlacklistType &= _blacklistType;
                // If all valid triggers are blacklisted, remove it
                if (typeTrigger.DamageType.HasFlag(typeTrigger.BlacklistType))
                {
                    EWCLogger.Warning(GetType().Name + " cannot have a hit trigger damage type matching " + _blacklistType.ToString());
                    Trigger.Activate.RemoveAt(i);
                    continue;
                }
            }

            if (!Trigger.Activate.Any())
                Trigger = null;
        }
    }
}
