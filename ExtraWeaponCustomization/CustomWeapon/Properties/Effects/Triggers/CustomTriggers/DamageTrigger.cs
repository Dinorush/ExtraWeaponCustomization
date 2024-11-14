using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class DamageTrigger : DamageTypeTrigger<WeaponPreHitDamageableContext>
    {
        public float Cap { get; private set; } = 0f;
        public DamageTrigger(DamageType type = DamageType.Any) : base(TriggerName.Damage, type) {}

        public override float Invoke(WeaponTriggerContext context)
        {
            if (context is WeaponPreHitDamageableContext hitContext
                && !hitContext.DamageType.HasAnyFlag(BlacklistType)
                && hitContext.DamageType.HasFlag(DamageType))
            {
                if (Cap > 0)
                    return Math.Min(Cap, hitContext.Damage);
                return hitContext.Damage;
            }
            return 0f;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "cap":
                    Cap = reader.GetSingle();
                    break;
            }
        }
    }
}
