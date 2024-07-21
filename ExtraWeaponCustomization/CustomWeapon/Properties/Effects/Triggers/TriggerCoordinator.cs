using System;
using System.Collections.Generic;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Linq;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class TriggerCoordinator
    {
        private static readonly Random Random = new();
        public ITriggerCallback? Parent { get; set; }
        public List<ITrigger> Activate { get; set; } = new();
        public float Cooldown { get; set; } = 0f;
        public float Chance { get; set; } = 1f;

        public List<ITrigger>? Apply { get; set; }
        public float CooldownOnApply { get; set; } = 0f;

        public List<ITrigger>? Reset { get; set; }

        private float _nextActivateTime = 0f;
        private readonly List<TriggerContext> _accumulatedTriggers = new();

        public TriggerCoordinator(params ITrigger[] triggers)
        {
            Activate.AddRange(triggers);
        }

        public TriggerCoordinator Clone()
        {
            // Only need to shallow copy the triggers themselves since they hold no state information
            TriggerCoordinator result = new()
            {
                Activate = new(Activate),
                Apply = Apply != null ? new(Apply) : null,
                Reset = Reset != null ? new(Reset) : null,
                Cooldown = Cooldown,
                Chance = Chance,
                CooldownOnApply = CooldownOnApply
            };
            return result;
        }

        public void Invoke(WeaponTriggerContext context)
        {
            // Store valid activations (if any)
            if (Clock.Time >= _nextActivateTime && (Chance == 1f || Chance > Random.NextDouble()))
            {
                foreach (ITrigger trigger in Activate)
                {
                    float triggerAmt = trigger.Invoke(context);
                    if (triggerAmt > 0f)
                    {
                        _accumulatedTriggers.Add(new TriggerContext { triggerAmt = triggerAmt, context = context });
                        _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + Cooldown);
                    }
                }
            }

            // Apply stored activations. If there are no Apply triggers, stored activations apply immediately
            if (Apply?.Any(trigger => trigger.Invoke(context) > 0) ?? _accumulatedTriggers.Any() == true)
                ApplyTriggers();

            // Reset stored activations AND any related behavior on the callback this coordinator is tied to
            if (Reset?.Any(trigger => trigger.Invoke(context) > 0) == true)
                ResetTriggers();
        }

        private void ApplyTriggers()
        {
            // Need to copy list and reset prior to applying trigger in case applying triggers causes another application
            List<TriggerContext> temp = new(_accumulatedTriggers);
            _accumulatedTriggers.Clear();
            Parent?.TriggerApply(temp);
            _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + CooldownOnApply);
        }

        private void ResetTriggers()
        {
            _accumulatedTriggers.Clear();
            Parent?.TriggerReset();
        }
    }
}
