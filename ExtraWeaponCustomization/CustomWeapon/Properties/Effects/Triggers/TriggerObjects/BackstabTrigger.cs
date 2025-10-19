using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class BackstabTrigger : DamageableTrigger<WeaponHitDamageableContext>
    {
        public float Min { get; private set; } = 0f;
        public float Max { get; private set; } = 1f;
        public float Exponent { get; private set; } = 1f;

        public BackstabTrigger(params DamageType[] types) : base(TriggerName.Backstab, types) {}

        protected override float InvokeInternal(WeaponHitDamageableContext context)
        {
            return Amount * (context.OrigBackstab - 1).Map(MinBackstabRequired, MaxBackstabRequired, Min, Max, Exponent);
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "min":
                    Min = reader.GetSingle();
                    break;
                case "max":
                    Max = reader.GetSingle();
                    break;
                case "minrequired":
                case "minreq":
                case "maxrequired":
                case "maxreq":
                    base.DeserializeProperty(property[..3] + "backstab" + property[3..], ref reader);
                    break;
                default:
                    base.DeserializeProperty(property, ref reader);
                    break;
            }
        }
    }
}
