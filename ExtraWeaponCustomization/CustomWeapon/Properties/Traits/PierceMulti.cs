using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class PierceMulti : 
        Trait,
        IWeaponProperty<WeaponHitDamageableContext>
    {
        private static readonly DamageType[] BulletType = new[] { DamageType.Bullet };

        public DamageType[] ModDamageType { get; private set; } = BulletType;
        public float PierceDamageMulti { get; private set; } = 1f;

        public void Invoke(WeaponHitDamageableContext context)
        {
            if (context.DamageType.HasFlag(DamageType.Bullet))
                context.ShotInfo.Orig.Mod.Add(this, StatType.Damage, PierceDamageMulti, 0f, StackType.Multiply, StackType.Multiply, null, ModDamageType);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(PierceDamageMulti), PierceDamageMulti);
            writer.WriteString(nameof(ModDamageType), ModDamageType[0].ToString());
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property.ToLowerInvariant())
            {
                case "piercedamagemulti":
                case "piercemulti":
                case "multi":
                    PierceDamageMulti = reader.GetSingle();
                    break;
                case "moddamagetype":
                case "damagetype":
                    ModDamageType = reader.GetString().ToDamageTypes();
                    break;
                default:
                    break;
            }
        }
    }
}
