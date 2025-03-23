using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class KillTrigger : DamageTypeTrigger<WeaponPostKillContext>
    {
        public float MaxDelay { get; set; } = 0.5f;
        public bool RequireDidKill { get; set; } = true;

        public KillTrigger(params DamageType[] types) : base(TriggerName.Kill, types) { }

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (!base.Invoke(context, out amount)) return false;

            var killContext = (WeaponPostKillContext)context;
            if (killContext.Delay < MaxDelay && (!RequireDidKill || killContext.DidKill))
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
                    RequireDidKill = reader.GetBoolean();
                    break;
            }
        }
    }
}
