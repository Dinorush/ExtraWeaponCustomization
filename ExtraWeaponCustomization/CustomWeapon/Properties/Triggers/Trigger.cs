using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Triggers
{
    public abstract class Trigger : IContextCallback
    {
        public bool AllowStack { get; } = true;

        public abstract IContextCallback Clone();
        public abstract void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options);
        public abstract void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }
}
