using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Patches.Melee;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ChargeTrigger : DamageTypeTrigger<WeaponPreHitEnemyContext>
    {
        public float Min { get; set; } = 0f;
        public float Max { get; set; } = 1f;
        public bool Scale { get; set; } = true;
        public ChargeTrigger(DamageType type = DamageType.Any) : base(ITrigger.Charge, type) {}

        public override float Invoke(WeaponTriggerContext context)
        {
            if (context is WeaponPreHitEnemyContext hitContext
                && !hitContext.DamageType.HasFlag(BlacklistType)
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
