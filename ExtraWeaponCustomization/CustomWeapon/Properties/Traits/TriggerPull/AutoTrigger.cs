using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class AutoTrigger : 
        Trait,
        IWeaponProperty<WeaponPostSetupContext>
    {
        public void Invoke(WeaponPostSetupContext context) {
            CWC.Weapon.m_archeType.m_triggerNeedsPress = false;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public override IWeaponProperty Clone()
        {
            return new AutoTrigger();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader) {}
    }
}
