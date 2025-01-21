using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class DamageTrigger : DamageableTrigger<WeaponHitDamageableContext>
    {
        public float Cap { get; private set; } = 0f;
        public DamageTrigger(DamageType type = DamageType.Any) : base(TriggerName.Damage, type) {}

        protected override float InvokeInternal(WeaponHitDamageableContext context)
        {
            return Cap > 0 ? Math.Min(Cap, context.Damage * Amount) : context.Damage * Amount;
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
