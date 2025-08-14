using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class StaggerTrigger : HitTrackerTrigger<WeaponPostStaggerContext>
    {
        public bool IncludeLimbBreak { get; set; } = true;

        public StaggerTrigger(params DamageType[] types) : base(TriggerName.Stagger, types) { }

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (!base.Invoke(context, out amount)) return false;

            var staggerContext = (WeaponPostStaggerContext)context;
            if (!staggerContext.LimbBreak || IncludeLimbBreak)
                return true;

            amount = 0;
            return false;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "includelimbbreak":
                case "limbbreak":
                    IncludeLimbBreak = reader.GetBoolean();
                    break;
            }
        }
    }
}
