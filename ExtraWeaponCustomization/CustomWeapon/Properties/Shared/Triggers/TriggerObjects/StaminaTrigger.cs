using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Shared.Triggers
{
    public sealed class StaminaTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.Stamina;
        public float Amount { get; private set; } = 1f;
        public float AmountAtMin { get; private set; } = 1f;
        public float AmountAtMax { get; private set; } = 1f;
        public float StaminaMinRel { get; private set; } = 0f;
        public float StaminaMaxRel { get; private set; } = 1f;
        public float Exponent { get; private set; } = 1f;
        public bool FlipExponent { get; private set; } = false;
        public bool AllowBeyondBounds { get; private set; } = true;

        public StaminaTrigger() {}

        public bool StoreZeroAmount => true;

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (context is WeaponStaminaContext stamContext)
            {
                amount = CalculateAmount(stamContext.Stamina);
                return true;
            }
            amount = 0f;
            return false;
        }

        private float CalculateAmount(float percent)
        {
            if (!AllowBeyondBounds && (percent < StaminaMinRel || percent > StaminaMaxRel))
                return 0;

            if (FlipExponent)
                return Amount * percent.MapInverted(StaminaMinRel, StaminaMaxRel, AmountAtMin, AmountAtMax, Exponent);
            else
                return Amount * percent.Map(StaminaMinRel, StaminaMaxRel, AmountAtMin, AmountAtMax, Exponent);
        }

        public void Reset() { }

        public ITrigger Clone() => this;

        public bool DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
                case "triggeramountatmin":
                case "amountatmin":
                    AmountAtMin = reader.GetSingle();
                    break;
                case "triggeramountatmax":
                case "amountatmax":
                    AmountAtMax = reader.GetSingle();
                    break;
                case "staminaminrel":
                case "staminamin":
                    StaminaMinRel = reader.GetSingle();
                    break;
                case "staminamaxrel":
                case "staminamax":
                    StaminaMaxRel = reader.GetSingle();
                    break;
                case "exponent":
                    Exponent = reader.GetSingle();
                    break;
                case "flipexponent":
                case "flip":
                    FlipExponent = reader.GetBoolean();
                    break;
                case "allowbeyondbounds":
                    AllowBeyondBounds = reader.GetBoolean();
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
