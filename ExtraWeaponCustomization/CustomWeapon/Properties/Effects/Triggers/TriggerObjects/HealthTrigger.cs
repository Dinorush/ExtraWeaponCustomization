using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class HealthTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.Health;
        public float Amount { get; private set; } = 1f;
        public float AmountAtMin { get; private set; } = 1f;
        public float AmountAtMax { get; private set; } = 1f;
        public float HealthMinRel { get; private set; } = 0f;
        public float HealthMaxRel { get; private set; } = 1f;
        public float Exponent { get; private set; } = 1f;
        public bool FlipExponent { get; private set; } = false;

        public HealthTrigger() {}

        public bool StoreZeroAmount => true;

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (context is WeaponHealthContext healthContext)
            {
                amount = CalculateAmount(healthContext.Health, healthContext.HealthMax);
                return true;
            }
            else if (context is WeaponInitContext initContext)
            {
                var player = initContext.Owner.Player;
                amount = CalculateAmount(player.Damage.Health, player.Damage.HealthMax);
                return true;
            }
            amount = 0f;
            return false;
        }

        private float CalculateAmount(float health, float healthMax)
        {
            if (FlipExponent)
                return Amount * (health / healthMax).MapInverted(HealthMinRel, HealthMaxRel, AmountAtMin, AmountAtMax, Exponent);
            else
                return Amount * (health / healthMax).Map(HealthMinRel, HealthMaxRel, AmountAtMin, AmountAtMax, Exponent);
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
                case "healthminrel":
                case "healthmin":
                    HealthMinRel = reader.GetSingle();
                    break;
                case "healthmaxrel":
                case "healthmax":
                    HealthMaxRel = reader.GetSingle();
                    break;
                case "exponent":
                    Exponent = reader.GetSingle();
                    break;
                case "flipexponent":
                case "flip":
                    FlipExponent = reader.GetBoolean();
                    break;
            }
        }
    }
}
