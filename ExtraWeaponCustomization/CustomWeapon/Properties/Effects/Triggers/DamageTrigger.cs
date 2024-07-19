using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class DamageTrigger : DamageFlagTrigger<WeaponPreHitEnemyContext>
    {
        public float Cap { get; set; }
        public DamageTrigger(DamageFlag type = DamageFlag.Any) : base(ITrigger.Damage, type) {}

        public override float Invoke(WeaponTriggerContext context)
        {
            if (context is WeaponPreHitEnemyContext hitContext && hitContext.DamageFlag.HasFlag(Type))
            {
                if (Cap > 0)
                    return Math.Min(Cap, hitContext.Damage);
                return hitContext.Damage;
            }
            return 0f;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            base.DeserializeProperty(property, ref reader, options);
            switch (property)
            {
                case "cap":
                    Cap = reader.GetSingle();
                    break;
            }
        }
    }
}
