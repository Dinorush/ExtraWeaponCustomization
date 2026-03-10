using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ChargeEndTrigger : ITrigger
    {
        public TriggerName Name => TriggerName.ChargeEnd;

        public float Amount { get; private set; } = 1f;
        public float Min { get; private set; } = 0f;
        public float Max { get; private set; } = 1f;
        public float Exponent { get; private set; } = 3f;
        public float MinRequired { get; private set; } = 0f;
        public float MaxRequired { get; private set; } = 1f;

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is not WeaponChargeEndContext endContext) return false;

            float charge = endContext.Charge;
            if (charge >= MinRequired && charge <= MaxRequired)
            {
                charge = charge.Map(MinRequired, MaxRequired, Min, Max, Exponent);
                amount = charge * Amount;
                return true;
            }
            return false;
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
                case "min":
                    Min = reader.GetSingle();
                    break;
                case "max":
                    Max = reader.GetSingle();
                    break;
                case "exponent":
                    Exponent = reader.GetSingle();
                    break;
                case "minrequired":
                case "minreq":
                    MinRequired = reader.GetSingle();
                    break;
                case "maxrequired":
                case "maxreq":
                    MaxRequired = reader.GetSingle();
                    break;
            }
        }
    }
}
