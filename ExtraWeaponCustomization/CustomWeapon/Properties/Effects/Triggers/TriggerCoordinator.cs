using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;
using EWC.Utils;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class TriggerCoordinator
    {
        public ITriggerCallback? Parent { get; set; }
        public ActivateHolder Activate { get; private set; }
        public ResetHolder? Reset { get; private set; }
        public bool ResetPreviousOnly { get; private set; } = false;

        public TriggerCoordinator(params ITrigger[] triggers)
        {
            Activate = new(this, triggers);
        }

        public TriggerCoordinator Clone()
        {
            var copy = CopyUtil.Clone(this);
            copy.Activate = (ActivateHolder) Activate.Clone(copy);
            copy.Reset = (ResetHolder?) Reset?.Clone(copy);
            return copy;
        }

        public void OnReferenceSet()
        {
            Activate.OnReferenceSet();
            Reset?.OnReferenceSet();
        }

        public void Invoke(WeaponTriggerContext context)
        {
            // Store valid activations (if any) and whether they should be applied.
            bool apply = Activate.Invoke(context);

            // Check if we will want to reset.
            // Necessary to check before applying for ResetPreviousOnly to function correctly.
            bool reset = Reset?.Invoke(context) ?? false;

            // Similar to a standard reset, but fields needed to apply stored activations are preserved
            if (ResetPreviousOnly && reset)
                Reset!.ResetTriggers(!apply);

            // Apply stored activations. If there are no Apply triggers, stored activations apply immediately
            if (apply)
                Activate.ApplyTriggers();

            // Reset stored activations AND any related behavior on the callback this coordinator is tied to
            if (!ResetPreviousOnly && reset)
                Reset!.ResetTriggers();
        }

        public void ForceReset()
        {
            Parent?.TriggerReset();
            Activate.Reset();
            Reset?.Reset();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "activate":
                case "triggers":
                case "trigger":
                    Activate.DeserializeTriggerList(ref reader);
                    break;
               
                case "resetpreviousonly":
                case "resetprevious":
                    ResetPreviousOnly = reader.GetBoolean();
                    break;

                case string name when name.StartsWith("reset"):
                    Reset ??= new(this);
                    if (property == "reset")
                        Reset.DeserializeTriggerList(ref reader);
                    else
                        Reset.DeserializeProperty(property["reset".Length..], ref reader);
                    break;

                default:
                    // Activate is the default and doesn't require a prefix for its fields
                    if (property.StartsWith("activate"))
                        property = property["activate".Length..];
                    Activate.DeserializeProperty(property, ref reader);
                    break;
            }
        }

        public static TriggerCoordinator? Deserialize(ref Utf8JsonReader reader, bool allowEmptyActivate = false)
        {
            var coordinator = JSON.EWCJson.Deserialize<TriggerCoordinator>(ref reader);
            if (coordinator == null) return null;
            return allowEmptyActivate || coordinator.Activate.Triggers.Count != 0 ? coordinator : null;
        }
    }
}
