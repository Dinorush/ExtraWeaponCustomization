using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class InitTrigger : ITrigger
    {
        public TriggerName Name => TriggerName.Init;
        public float Amount { get; private set; } = 1f;

        private readonly PlayerType _type;

        public InitTrigger(string jsonName)
        {
            jsonName = jsonName.ToLower();
            if (jsonName.Contains("client"))
                _type = PlayerType.Client;
            else if (jsonName.Contains("host"))
                _type = PlayerType.Host;
            else
                _type = PlayerType.Any;
        }

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (_type != PlayerType.Any && SNetwork.SNet.IsMaster != (_type == PlayerType.Host))
                return false;

            if (context is WeaponInitContext)
            {
                amount = Amount;
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
            }
        }

        enum PlayerType
        {
            Any,
            Host,
            Client
        }
    }
}
