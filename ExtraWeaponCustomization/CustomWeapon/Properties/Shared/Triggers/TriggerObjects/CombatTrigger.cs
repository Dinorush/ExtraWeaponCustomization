using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Shared.Triggers
{
    public sealed class CombatTrigger : ITrigger
    {
        public TriggerName Name => TriggerName.Combat;
        public float Amount { get; private set; } = 1f;

        private readonly bool _inCombat;

        public CombatTrigger(bool inCombat)
        {
            _inCombat = inCombat;
        }

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is WeaponCombatContext combatContext && _inCombat == combatContext.InCombat)
            {
                amount = Amount;
                return true;
            }
            return false;
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
                default:
                    return false;
            }
            return true;
        }
    }
}
