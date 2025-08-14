using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class HitTrackerTrigger<TContext> : DamageTypeTrigger<TContext> where TContext : WeaponHitTrackerContextBase
    {
        public float MaxDelay { get; private set; } = 0.5f;
        public bool RequireLastHit { get; private set; } = true;

        public HitTrackerTrigger(TriggerName name, params DamageType[] types) : base(name, types) { }

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (!base.Invoke(context, out amount)) return false;

            var killContext = (TContext)context;
            if (killContext.Delay < MaxDelay && (!RequireLastHit || killContext.DidLastHit))
                return true;

            amount = 0;
            return false;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "maxdelay":
                case "delay":
                    MaxDelay = reader.GetSingle();
                    break;
                case "requiredidkill":
                case "requirekill":
                case "requirelasthit":
                    RequireLastHit = reader.GetBoolean();
                    break;
            }
        }
    }
}
