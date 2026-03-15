using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class PlayerDamageTypeTrigger : ITrigger
    {
        public PlayerDamageType[] DamageTypes { get; }
        public TriggerName Name { get; private set; }
        public float Amount { get; private set; } = 1f;

        public PlayerDamageTypeTrigger(TriggerName name, params PlayerDamageType[] types)
        {
            Name = name;
            DamageTypes = types.Length == 0 ? PlayerDamageTypeConst.Any : types;
        }

        public virtual bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is WeaponDamageTakenContext hitContext && hitContext.DamageType.HasFlagIn(DamageTypes))
            {
                amount = Amount;
                return true;
            }
            return false;
        }

        public void Reset() { }

        public ITrigger Clone() => this;

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
            }
        }
    }
}
