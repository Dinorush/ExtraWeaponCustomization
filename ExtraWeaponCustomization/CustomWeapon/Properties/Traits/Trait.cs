using Gear;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public abstract class Trait : IWeaponProperty
    {
#pragma warning disable CS8618 // Set when registered to a CWC
        public CustomWeaponComponent CWC { get; set; }
#pragma warning restore CS8618

        public abstract IWeaponProperty Clone();
        public abstract void Serialize(Utf8JsonWriter writer);
        public abstract void DeserializeProperty(string property, ref Utf8JsonReader reader);
    }
}
