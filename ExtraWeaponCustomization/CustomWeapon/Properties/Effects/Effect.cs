using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Collections.Generic;
using System.Text.Json;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using System.Linq;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public abstract class Effect : 
        ITriggerCallback,
        IWeaponProperty<WeaponTriggerContext>
    {
        public bool AllowStack { get; } = true;
        public TriggerCoordinator? Trigger { get; set; }

        public void Invoke(WeaponTriggerContext context) => Trigger?.Invoke(context);

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
                case "trigger":
                    Trigger = JsonSerializer.Deserialize<TriggerCoordinator>(ref reader, options);
                    break;
            }
        }

        protected void VerifyTrigger(params string[] names)
        {
            if (Trigger == null) return;

            for (int i = Trigger.Activate.Count - 1; i >= 0; i--)
            {
                string name = Trigger.Activate[i].Name;
                // If the trigger isn't of the valid class, remove it
                if (!names.Contains(name))
                    Trigger.Activate.RemoveAt(i);
            }

            if (!Trigger.Activate.Any())
                Trigger = null;
        }

        protected void BlacklistTriggerFlag(DamageFlag flag)
        {
            if (Trigger == null) return;

            for (int i = Trigger.Activate.Count - 1; i >= 0; i--)
            {
                if (Trigger.Activate[i] is not IDamageFlagTrigger flagTrigger) continue;

                flagTrigger.BlacklistType |= flag;
                // If all valid triggers are blacklisted, remove it
                if (flagTrigger.Type.HasFlag(flagTrigger.BlacklistType))
                    Trigger.Activate.RemoveAt(i);
            }

            if (!Trigger.Activate.Any())
                Trigger = null;
        }
    }
}
