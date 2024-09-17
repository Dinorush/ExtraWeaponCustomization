using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties
{
    public interface IWeaponProperty
    {
        public CustomWeaponComponent CWC { get; internal set; }
        IWeaponProperty Clone(); // Should return a new instance with the same initial data.
        void Serialize(Utf8JsonWriter writer);
        void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }

    public interface IWeaponProperty<TContext> : IWeaponProperty where TContext : IWeaponContext
    {
        void Invoke(TContext context);
    }

    // Skeleton interfaces used to identify gun-specific or melee-specific properties
    public interface IGunProperty { }

    public interface IMeleeProperty { }
}
