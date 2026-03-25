using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class KeyTrigger : ITrigger
    {
        public TriggerName Name => TriggerName.Key;
        public float Amount { get; private set; } = 1f;
        public KeyCode Key { get; }
        public bool OnDown { get; }

        public KeyTrigger(KeyCode key, bool onDown)
        {
            Key = key;
            OnDown = onDown;
        }

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is WeaponKeyContext keyContext && Key == keyContext.Key && OnDown == keyContext.IsDown)
            {
                amount = Amount;
                return true;
            }
            return false;
        }

        public void Reset() { }

        public void OnPropertiesSetup(CustomWeaponComponent cwc)
        {
            cwc.RegisterKeyWatcher(Key);
        }

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
    }
}
