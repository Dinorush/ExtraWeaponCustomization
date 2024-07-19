using System;
using System.Collections.Generic;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Linq;
using System.Text.Json;

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

        public TriggerCoordinator Clone()
        {
            TriggerCoordinator result = new();
            result.Activate = new(Activate);
            if (Apply != null)
                result.Apply = new(Apply);
            if (Reset != null)
                result.Reset = new(Reset);

            result.Cooldown = Cooldown;
            result.CooldownOnApply = CooldownOnApply;
            return result;
        }

        public void Invoke(WeaponTriggerContext context)
        {
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

            if (Apply?.Any(trigger => trigger.Invoke(context) > 0) ?? _accumulatedTriggers.Any() == true)
                ApplyTriggers();

            if (Reset?.Any(trigger => trigger.Invoke(context) > 0) == true)
                ResetTriggers();
        }

        private void ApplyTriggers()
        {
            Parent?.TriggerApply(_accumulatedTriggers);
            _accumulatedTriggers.Clear();
            _nextActivateTime = Math.Max(_nextActivateTime, Clock.Time + CooldownOnApply);
        }

        private void ResetTriggers()
        {
            Parent?.TriggerReset();
            _accumulatedTriggers.Clear();
        }
    }
}
