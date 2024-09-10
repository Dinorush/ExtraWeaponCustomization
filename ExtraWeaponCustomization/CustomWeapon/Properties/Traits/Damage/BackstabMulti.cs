using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    internal class BackstabMulti : 
        Trait,
        IWeaponProperty<WeaponBackstabContext>
    {
        public float BackstabDamageMulti { get; set; } = 1f;

        public void Invoke(WeaponBackstabContext context)
        {
            context.AddMod(BackstabDamageMulti, StackType.Multiply);
        }

        public override IWeaponProperty Clone()
        {
            BackstabMulti copy = new()
            {
                BackstabDamageMulti = BackstabDamageMulti
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(BackstabDamageMulti), BackstabDamageMulti);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "backstabdamagemulti":
                case "backdamagemulti":
                case "backstabmulti":
                case "backmulti":
                case "multi":
                    BackstabDamageMulti = reader.GetSingle();
                    break;
            }
        }
    }
}
