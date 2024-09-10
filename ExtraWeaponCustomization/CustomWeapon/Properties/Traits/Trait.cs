using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public abstract class Trait : IWeaponProperty
    {
        public bool AllowStack { get; } = false;

        public abstract IWeaponProperty Clone();
        public abstract void Serialize(Utf8JsonWriter writer);
        public abstract void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }
}
