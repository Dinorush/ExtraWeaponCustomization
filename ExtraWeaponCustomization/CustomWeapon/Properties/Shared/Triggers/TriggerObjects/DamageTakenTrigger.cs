using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Shared.Triggers
{
    public sealed class DamageTakenTrigger : PlayerDamageTypeTrigger
    {
        public float Cap { get; private set; } = 0f;

        public DamageTakenTrigger(PlayerDamageType[] damageTypes) : base(TriggerName.DamageTaken, damageTypes) { }

        public override bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (base.Invoke(context, out amount))
            {
                float damage = ((WeaponDamageTakenContext)context).Damage;
                amount = Cap > 0f ? Math.Min(Cap, amount * damage) : Amount * damage;
                return true;
            }
            return false;
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
