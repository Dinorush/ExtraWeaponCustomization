using EWC.CustomWeapon.WeaponContext;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties
{
    public interface IWeaponProperty
    {
        public CustomWeaponComponent CWC { get; set; }
        public bool ShouldRegister(Type contextType) => true;
        WeaponPropertyBase Clone(); // Should return a new instance with the same initial data.
        void Serialize(Utf8JsonWriter writer);
        void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }

    public interface IWeaponProperty<TContext> : IWeaponProperty where TContext : IWeaponContext
    {
        void Invoke(TContext context);
    }

    // For networked properties to send only their sync ID so their fields can be retrieved locally.
    public interface ISyncProperty : IWeaponProperty
    {
        public ushort SyncPropertyID { get; set; }
    }
}
