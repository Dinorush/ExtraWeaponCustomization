using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Patches.Melee;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ChargeLandedTrigger : DamageTypeTrigger<WeaponHitContextBase>
    {
        public float Min { get; private set; } = 0f;
        public float Max { get; private set; } = 1f;
        public float Exponent { get; private set; } = 3f;
        public float MinRequired { get; private set; } = 0f;
        public float MaxRequired { get; private set; } = 1f;

        public ChargeLandedTrigger(params DamageType[] damageTypes) : base(TriggerName.ChargeLanded, damageTypes) { }

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            float charge = MeleePatches.CachedCharge;
            // Want to trigger when a melee hit lands but NOT on a pre-hit context
            if (charge >= MinRequired && charge <= MaxRequired &&
                base.Invoke(context, out amount) && (context is WeaponHitContext || context is WeaponHitDamageableContext))
            {
                charge = charge.Map(MinRequired, MaxRequired, Min, Max, Exponent);
                amount *= charge;
                return true;
            }
            amount = 0;
            return false;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "min":
                    Min = reader.GetSingle();
                    break;
                case "max":
                    Max = reader.GetSingle();
                    break;
                case "exponent":
                    Exponent = reader.GetSingle();
                    break;
                case "minrequired":
                case "minreq":
                    MinRequired = reader.GetSingle();
                    break;
                case "maxrequired":
                case "maxreq":
                    MaxRequired = reader.GetSingle();
                    break;
            }
        }
    }
}
