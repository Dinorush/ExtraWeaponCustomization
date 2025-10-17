using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class ChargeExponent :
        Trait,
        IWeaponProperty<WeaponChargeContext>
    {
        public float Exponent { get; private set; } = 3f;

        protected override WeaponType RequiredWeaponType => WeaponType.Melee;

        public void Invoke(WeaponChargeContext context)
        {
            context.Exponent = Exponent;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Exponent), Exponent);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property.ToLowerInvariant())
            {
                case "exponent":
                    Exponent = reader.GetSingle();
                    break;
                default:
                    break;
            }
        }
    }
}
