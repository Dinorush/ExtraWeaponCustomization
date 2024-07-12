using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class AutoTrigger : 
        Trait,
        IContextCallback<WeaponPostSetupContext>
    {
        public void Invoke(WeaponPostSetupContext context) {
            context.Weapon.m_archeType.m_triggerNeedsPress = false;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public override IContextCallback Clone()
        {
            return new AutoTrigger();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader) {}
    }
}
