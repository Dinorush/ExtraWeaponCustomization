using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties
{
    public interface IWeaponProperty
    {
        bool AllowStack { get; }
        void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options);
        void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }

    public interface IWeaponProperty<TContext> : IWeaponProperty where TContext : IWeaponContext
    {
        void Invoke(TContext context);
    }
}
