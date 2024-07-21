using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using System.Linq;
using ExtraWeaponCustomization.Utils;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public abstract class Effect : 
        ITriggerCallback,
        IWeaponProperty<WeaponTriggerContext>
    {
        public bool AllowStack { get; } = true;

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

        // Backwards compatibility with pre-Trigger overhaul | Remove when no longer supported
        private float _cooldown = 0f;
        private ITrigger? _resetTrigger;

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

        public abstract void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options);
        protected void SerializeTrigger(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            if (Trigger != null)
            {
                writer.WritePropertyName(nameof(Trigger));
                JsonSerializer.Serialize(writer, Trigger, options);
            }
            else
                writer.WriteString(nameof(Trigger), "Invalid");
        }

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch(property)
            {
                case "triggertype":
                case "trigger":
                    Trigger = JsonSerializer.Deserialize<TriggerCoordinator>(ref reader, options);
                    VerifyTriggers();
                    break;
                case "resettriggertype":
                case "resettrigger":
                    _resetTrigger = JsonSerializer.Deserialize<ITrigger>(ref reader, options);
                    EWCLogger.Warning(
                        "\"ResetTrigger\" as an Effect field is deprecated and will not be supported in a future version. " +
                        "Please port it to the Trigger object."
                        );
                    break;
                case "cooldown":
                    _cooldown = reader.GetSingle();
                    EWCLogger.Warning(
                        "\"Cooldown\" as an Effect field is deprecated and will not be supported in a future version. " +
                        "Please port it to the Trigger object."
                        );
                    break;
            }

            // Backwards compatibility with pre-Trigger overhaul | Remove when no longer supported
            if (Trigger != null)
            {
                if (_cooldown > 0)
                {
                    Trigger.Cooldown = _cooldown;
                    _cooldown = 0;
                }

                if (_resetTrigger != null)
                {
                    Trigger.Reset = new() { _resetTrigger };
                    _resetTrigger = null;
                }
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
                }

                if (Trigger.Activate[i] is not IDamageTypeTrigger typeTrigger) continue;

                typeTrigger.BlacklistType &= _blacklistType;
                // If all valid triggers are blacklisted, remove it
                if (typeTrigger.DamageType.HasFlag(typeTrigger.BlacklistType))
                {
                    EWCLogger.Warning(GetType().Name + " cannot have a hit trigger damage type matching " + _blacklistType.ToString());
                    Trigger.Activate.RemoveAt(i);
                }
            }

            if (!Trigger.Activate.Any())
                Trigger = null;
        }
    }
}
