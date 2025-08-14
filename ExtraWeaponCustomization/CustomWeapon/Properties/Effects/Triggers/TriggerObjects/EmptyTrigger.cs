using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class EmptyTrigger : ITrigger
    {
        public static readonly EmptyTrigger Instance = new();

        public TriggerName Name { get; } = TriggerName.Empty;

        public bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            return false;
        }

        public void Reset() { }

        public ITrigger Clone() => this;

        public void DeserializeProperty(string property, ref Utf8JsonReader reader) {}
    }
}
