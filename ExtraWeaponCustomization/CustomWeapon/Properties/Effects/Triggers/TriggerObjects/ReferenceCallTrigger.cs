using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class ReferenceCallTrigger : ITrigger
    {
        public TriggerName Name { get; } = TriggerName.Reference;
        public float Amount { get; private set; } = 1f;
        public uint ID { get; private set; } = 0u;
        public string Callback { get; private set; } = string.Empty;

        private int _callbackID = 0;

        public ReferenceCallTrigger(string id) => ID = WeaponPropertyBase.StringIDToInt(id);
        public ReferenceCallTrigger(uint id) => ID = id;
        public ReferenceCallTrigger() { }

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            if (context is WeaponReferenceContext refContext && refContext.ID == ID && refContext.CallbackID == _callbackID)
            {
                amount = Amount;
                return true;
            }
            amount = 0f;
            return false;
        }

        public void Reset() { }

        public ITrigger Clone() => this;

        public void OnReferenceSet(CustomWeaponComponent cwc)
        {
            if (cwc.TryGetReference(ID, out var property))
            {
                if (property is ITriggerEvent triggerEvent)
                    _callbackID = triggerEvent.GetCallbackID(Callback);
                else
                    EWCLogger.Error($"Cannot use {property.GetType().Name} as a trigger!");
            }
            else
                EWCLogger.Error($"Unable to find property with ID {ID}!");
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
                case "referenceid":
                case "id":
                    if (reader.TokenType == JsonTokenType.String)
                        ID = WeaponPropertyBase.StringIDToInt(reader.GetString()!);
                    else
                        ID = reader.GetUInt32();
                    break;
                case "callback":
                    Callback = reader.GetString()!;
                    break;
            }
        }
    }
}
