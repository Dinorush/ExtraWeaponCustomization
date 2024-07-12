using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties
{
    public interface IContextCallback
    {
        bool AllowStack { get; }
        IContextCallback Clone(); // Should return a new instance with the same initial data.
        void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options);
        void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }

    public interface IContextCallback<TContext> : IContextCallback where TContext : IWeaponContext
    {
        void Invoke(TContext context);
    }
}
