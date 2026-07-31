using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Shared.Triggers
{
    public sealed class SentryStateTrigger : ITrigger
    {
        public TriggerName Name => TriggerName.SentryState;
        public float Amount { get; private set; } = 1f;

        private readonly bool _deployed;

        public SentryStateTrigger(bool deployed)
        {
            _deployed = deployed;
        }

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is WeaponSentryStateContext sentryContext && _deployed == sentryContext.Deployed)
            {
                amount = Amount;
                return true;
            }
            return false;
        }

        public void Reset() { }

        public ITrigger Clone() => this;

        public bool OnPropertiesSetup(CustomWeaponComponent cwc)
        {
            return cwc.Weapon.IsType(Enums.WeaponType.SentryHolder);
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
