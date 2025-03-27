using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class DamageTrigger : DamageableTrigger<WeaponHitDamageableContext>
    {
        public float Cap { get; private set; } = 0f;
        public bool ClampToHealth { get; private set; } = true;

        public DamageTrigger(params DamageType[] types) : base(TriggerName.Damage, types) {}

        protected override float InvokeInternal(WeaponHitDamageableContext context)
        {
            float damage = ClampToHealth ? context.DamageClamped : context.Damage;
            return Cap > 0 ? Math.Min(Cap, damage * Amount) : damage * Amount;
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "cap":
                    Cap = reader.GetSingle();
                    break;
                case "clamptohealth":
                case "clamp":
                    ClampToHealth = reader.GetBoolean();
                    break;
            }
        }
    }
}
