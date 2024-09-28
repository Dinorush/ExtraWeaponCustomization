using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public sealed class BasicTrigger<TContext> : ITrigger where TContext : WeaponTriggerContext
    {
        public TriggerName Name { get; }
        public BasicTrigger(TriggerName name)
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
