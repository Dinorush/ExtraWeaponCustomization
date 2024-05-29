using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class Explosive : IWeaponProperty<WeaponPreHitContext>
    {
        public readonly static string Name = typeof(Explosive).Name;
        public bool AllowStack { get; } = true;

        public float MaxDamage { get; set; } = 0f;
        public float MinDamage { get; set; } = 0f;
        public float InnerRadius { get; set; } = 0f;
        public float Radius { get; set; } = 0f;
        public float PrecisionMult { get; set; } = 0f;
        public float StaggerMult { get; set; } = 1f;
        public bool IgnoreFalloff { get; set; } = false;
        public bool DamageLimb { get; set; } = true;
        public bool IgnoreArmor { get; set; } = false;
        public bool IgnoreBackstab { get; set; } = false;
        public bool IgnoreDamageMods { get; set; } = false;

        public void Invoke(WeaponPreHitContext context)
        {
            if (!context.Weapon.Owner.IsLocallyOwned) return;

            float falloffMod = IgnoreFalloff ? 1f : context.Falloff;
            ExplosionManager.DoExplosion(context.Data.rayHit.point, context.Data.fireDir.normalized, context.Weapon.Owner, falloffMod, this, context.Weapon);
        }

        public IWeaponProperty Clone()
        {
            Explosive copy = new()
            {
                MaxDamage = MaxDamage,
                MinDamage = MinDamage,
                InnerRadius = InnerRadius,
                Radius = Radius,
                PrecisionMult = PrecisionMult,
                StaggerMult = StaggerMult,
                DamageLimb = DamageLimb,
                IgnoreArmor = IgnoreArmor,
                IgnoreFalloff = IgnoreFalloff,
                IgnoreBackstab = IgnoreBackstab,
                IgnoreDamageMods = IgnoreDamageMods
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(MaxDamage), MaxDamage);
            writer.WriteNumber(nameof(MinDamage), MinDamage);
            writer.WriteNumber(nameof(InnerRadius), InnerRadius);
            writer.WriteNumber(nameof(Radius), Radius);
            writer.WriteNumber(nameof(PrecisionMult), PrecisionMult);
            writer.WriteNumber(nameof(StaggerMult), StaggerMult);
            writer.WriteBoolean(nameof(IgnoreFalloff), IgnoreFalloff);
            writer.WriteBoolean(nameof(DamageLimb), DamageLimb);
            writer.WriteBoolean(nameof(IgnoreArmor), IgnoreArmor);
            writer.WriteBoolean(nameof(IgnoreBackstab), IgnoreBackstab);
            writer.WriteBoolean(nameof(IgnoreDamageMods), IgnoreDamageMods);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "maxdamage":
                    MaxDamage = reader.GetSingle();
                    break;
                case "mindamage":
                    MinDamage = reader.GetSingle();
                    break;
                case "innerradius":
                case "minradius":
                    InnerRadius = reader.GetSingle();
                    break;
                case "radius":
                case "maxradius":
                    Radius = reader.GetSingle();
                    break;
                case "precisionmult":
                case "precision":
                    PrecisionMult = reader.GetSingle();
                    break;
                case "staggermult":
                case "stagger":
                    StaggerMult = reader.GetSingle();
                    break;
                case "ignorefalloff":
                    IgnoreFalloff = reader.GetBoolean();
                    break;
                case "damagelimb":
                    DamageLimb = reader.GetBoolean();
                    break;
                case "ignorearmor":
                    IgnoreArmor = reader.GetBoolean();
                    break;
                case "ignorebackstab":
                case "ignorebackdamage":
                case "ignorebackbonus":
                    IgnoreBackstab = reader.GetBoolean();
                    break;
                case "ignoredamagemods":
                case "ignoredamagemod":
                    IgnoreDamageMods = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}
