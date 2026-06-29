using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Shared.Triggers
{
    public sealed class InfectionTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.Infection;
        public float Amount { get; private set; } = 1f;
        public float AmountAtMin { get; private set; } = 1f;
        public float AmountAtMax { get; private set; } = 1f;
        public float InfectionMinRel { get; private set; } = 0f;
        public float InfectionMaxRel { get; private set; } = 1f;
        public float Exponent { get; private set; } = 1f;
        public bool FlipExponent { get; private set; } = false;
        public bool AllowBeyondBounds { get; private set; } = true;

        public InfectionTrigger() {}

        public bool StoreZeroAmount => true;

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (context is WeaponInfectionContext infectContext)
            {
                amount = CalculateAmount(infectContext.Infection);
                return true;
            }
            else if (context is WeaponInitContext initContext)
            {
                var player = initContext.Owner.Player;
                if (player == null)
                {
                    amount = 0f;
                    return false;
                }
                amount = CalculateAmount(player.Damage.Infection);
                return true;
            }
            amount = 0f;
            return false;
        }

        private float CalculateAmount(float infection)
        {
            if (!AllowBeyondBounds && (infection < InfectionMinRel || infection > InfectionMaxRel))
                return 0;

            if (FlipExponent)
                return Amount * infection.MapInverted(InfectionMinRel, InfectionMaxRel, AmountAtMin, AmountAtMax, Exponent);
            else
                return Amount * infection.Map(InfectionMinRel, InfectionMaxRel, AmountAtMin, AmountAtMax, Exponent);
        }

        public void Reset() { }

        public ITrigger Clone() => this;

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
                case "infectionminrel":
                case "infectionmin":
                    InfectionMinRel = reader.GetSingle();
                    break;
                case "infectionmaxrel":
                case "infectionmax":
                    InfectionMaxRel = reader.GetSingle();
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
            }
        }
    }
}
