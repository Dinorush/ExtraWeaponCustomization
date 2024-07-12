﻿using ExtraWeaponCustomization.CustomWeapon.Properties.Triggers;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public abstract class Effect : IContextCallback
    {
        public bool AllowStack { get; } = true;
        public Trigger? Trigger { get; set; }

        public abstract void TriggerInvoke();
        public abstract IContextCallback Clone();
        public abstract void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options);
        public abstract void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }
}
