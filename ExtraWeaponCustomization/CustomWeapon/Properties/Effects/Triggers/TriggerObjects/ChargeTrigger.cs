using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Patches.Melee;
using EWC.Utils;
using EWC.Utils.Log;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ChargeTrigger : DamageableTrigger<WeaponHitDamageableContext>
    {
        public float Min { get; private set; } = 0f;
        public float Max { get; private set; } = 1f;
        public float Exponent { get; private set; } = 3f;
        public float MinRequired { get; private set; } = 0f;
        public float MaxRequired { get; private set; } = 1f;

        public ChargeTrigger(DamageType type = DamageType.Any) : base(TriggerName.Charge, type) {}

        protected override float InvokeInternal(WeaponHitDamageableContext context)
        {
            float charge = MeleePatches.CachedCharge;
            if (charge >= MinRequired && charge <= MaxRequired)
            {
                charge = charge.Map(MinRequired, MaxRequired, Min, Max, Exponent);
                return charge * Amount;
            }
            return 0f;
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
                case "expo":
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
                case "scale":
                    EWCLogger.Warning("Scale is a deprecated field on Charge triggers. Please use Min and Max fields instead.");
                    if (!reader.GetBoolean())
                    {
                        Min = 1f;
                        Max = 1f;
                    }
                    break;
            }
        }
    }
}
