using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    internal class TumorMulti : IWeaponProperty<WeaponDamageContext>
    {
        public bool AllowStack { get; } = false;

        public float TumorDamageMulti { get; set; } = 1f;

        public void Invoke(WeaponDamageContext context)
        {
            Dam_EnemyDamageLimb_Custom? tumor = context.Damageable.TryCast<Dam_EnemyDamageLimb_Custom>();
            if (tumor != null)
                context.AddMod(TumorDamageMulti, StackType.Multiply);
        }

        public IWeaponProperty Clone()
        {
            TumorMulti copy = new()
            {
                TumorDamageMulti = TumorDamageMulti
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(TumorDamageMulti), TumorDamageMulti);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "tumordamagemulti":
                case "tumormulti":
                    TumorDamageMulti = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
