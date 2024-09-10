using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    internal class TumorMulti :
        Trait,
        IWeaponProperty<WeaponDamageContext>
    {
        public float TumorDamageMulti { get; set; } = 1f;

        public void Invoke(WeaponDamageContext context)
        {
            Dam_EnemyDamageLimb_Custom? tumor = context.Damageable.TryCast<Dam_EnemyDamageLimb_Custom>();
            if (tumor != null)
                context.Precision.AddMod(TumorDamageMulti, StackType.Multiply);
        }

        public override IWeaponProperty Clone()
        {
            TumorMulti copy = new()
            {
                TumorDamageMulti = TumorDamageMulti
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(TumorDamageMulti), TumorDamageMulti);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "tumordamagemulti":
                case "tumormulti":
                case "multi":
                    TumorDamageMulti = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
