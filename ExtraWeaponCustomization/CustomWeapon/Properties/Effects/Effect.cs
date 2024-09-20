using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using System.Linq;
using ExtraWeaponCustomization.JSON;
using ExtraWeaponCustomization.Utils.Log;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public abstract class Effect : 
        ITriggerCallback
    {
#pragma warning disable CS8618 // Set when registered to a CWC
        public CustomWeaponComponent CWC { get; set; }
#pragma warning restore CS8618
        public ItemEquippable Weapon => CWC.Weapon;

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

        private string[]? _validTriggers;
        private DamageType _blacklistType = DamageType.Invalid;

        public void Invoke(WeaponTriggerContext context) => Trigger?.Invoke(context);

        protected void SetValidTriggers(DamageType flag = DamageType.Invalid, params string[] names)
        {
            _validTriggers = names;
            _blacklistType = flag;
            VerifyTriggers();
        }
        protected void SetValidTriggers(params string[] names) => SetValidTriggers(DamageType.Invalid, names);

        public abstract void TriggerApply(List<TriggerContext> triggerList);
        public abstract void TriggerReset();

        public abstract IWeaponProperty Clone();

        public abstract void Serialize(Utf8JsonWriter writer);
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

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
                string name = Trigger.Activate[i].Name;
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
