using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public abstract class Trait : IContextCallback
    {
        public bool AllowStack { get; } = false;

        public abstract IContextCallback Clone();
        public abstract void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options);
        public abstract void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }
}
