using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class TumorMulti :
        Trait,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponDamageContext>
    {
        public float TumorDamageMulti { get; private set; } = 1f;
        public bool BypassCap { get; private set; } = false;
        public bool OverrideWeakspotMulti { get; private set; } = false;

        public void Invoke(WeaponDamageContext context)
        {
            Dam_EnemyDamageLimb_Custom? tumor = context.Damageable.TryCast<Dam_EnemyDamageLimb_Custom>();
            if (tumor != null)
            {
                context.Precision.AddMod(OverrideWeakspotMulti ? TumorDamageMulti / tumor.m_weakspotDamageMulti : TumorDamageMulti, StackType.Multiply);
                context.BypassTumorCap = BypassCap;
            }
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(TumorDamageMulti), TumorDamageMulti);
            writer.WriteBoolean(nameof(BypassCap), BypassCap);
            writer.WriteBoolean(nameof(OverrideWeakspotMulti), OverrideWeakspotMulti);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
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
                case "overrideweakspotmulti":
                case "overridemulti":
                case "override":
                    OverrideWeakspotMulti = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}
