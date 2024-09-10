using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties
{
    public interface IWeaponProperty
    {
        bool AllowStack { get; }
        IWeaponProperty Clone(); // Should return a new instance with the same initial data.
        void Serialize(Utf8JsonWriter writer);
        void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }

    public interface IWeaponProperty<TContext> : IWeaponProperty where TContext : IWeaponContext
    {
        void Invoke(TContext context);
    }
}
