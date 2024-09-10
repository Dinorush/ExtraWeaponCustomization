using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class BasicTrigger<TContext> : ITrigger where TContext : WeaponTriggerContext
    {
        public string Name { get; }
        public BasicTrigger(string name)
        {
            Name = name;
        }
        public float Invoke(WeaponTriggerContext context)
        {
            return context is TContext ? 1f : 0f;
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader) { }
    }
}
