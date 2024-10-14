using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    internal class TumorMulti :
        Trait,
        IWeaponProperty<WeaponDamageContext>
    {
        public float TumorDamageMulti { get; private set; } = 1f;
        public bool BypassCap { get; private set; } = false;

        public void Invoke(WeaponDamageContext context)
        {
            Dam_EnemyDamageLimb_Custom? tumor = context.Damageable.TryCast<Dam_EnemyDamageLimb_Custom>();
            if (tumor != null)
            {
                context.Precision.AddMod(TumorDamageMulti, StackType.Multiply);
                context.BypassTumorCap = BypassCap;
            }
        }

        public override IWeaponProperty Clone()
        {
            TumorMulti copy = new()
            {
                TumorDamageMulti = TumorDamageMulti,
                BypassCap = BypassCap
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(TumorDamageMulti), TumorDamageMulti);
            writer.WriteBoolean(nameof(BypassCap), BypassCap);
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
                case "bypasstumorcap":
                case "bypasscap":
                    BypassCap = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}
