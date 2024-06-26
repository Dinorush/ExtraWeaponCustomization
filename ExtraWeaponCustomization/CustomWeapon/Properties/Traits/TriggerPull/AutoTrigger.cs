﻿using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class AutoTrigger : IWeaponProperty<WeaponPostSetupContext>
    {
        public bool AllowStack { get; } = false;

        public void Invoke(WeaponPostSetupContext context) {
            context.Weapon.m_archeType.m_triggerNeedsPress = false;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public IWeaponProperty Clone()
        {
            return new AutoTrigger();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader) {}
    }
}
