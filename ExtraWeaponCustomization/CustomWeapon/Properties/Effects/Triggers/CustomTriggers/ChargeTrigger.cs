using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Patches.Melee;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ChargeTrigger : DamageTypeTrigger<WeaponPreHitDamageableContext>
    {
        public float Min { get; private set; } = 0f;
        public float Max { get; private set; } = 1f;
        public bool Scale { get; private set; } = true;
        public ChargeTrigger(DamageType type = DamageType.Any) : base(TriggerName.Charge, type) {}

        public override float Invoke(WeaponTriggerContext context)
        {
            if (context is WeaponPreHitDamageableContext hitContext
                && !hitContext.DamageType.HasAnyFlag(BlacklistType)
                && hitContext.DamageType.HasFlag(DamageType))
            {
                float charge = MeleePatches.CachedCharge;
                if (charge >= Min && charge <= Max)
                    return Scale ? charge : 1f;
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
                case "scale":
                    Scale = reader.GetBoolean();
                    break;
            }
        }
    }
}
