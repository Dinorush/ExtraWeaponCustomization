using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils.Extensions;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ShotModDebuff : 
        TriggerModDebuff,
        IGunProperty,
        IMeleeProperty
    {
        public StatType StatType { get; private set; } = StatType.Damage;
        public DamageType[] DamageType { get; private set; } = DamageTypeConst.Any;

        protected override DebuffModifierBase AddModifier(IDamageable damageable)
        {
            return DebuffManager.AddShotModDebuff(damageable, 1f, StatType, StackLayer, DamageType, DebuffID);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(Mod), Mod);
            writer.WriteString(nameof(StatType), StatType.ToString());
            writer.WriteString(nameof(DamageType), DamageType[0].ToString());
            writer.WriteNumber(nameof(Cap), Cap);
            writer.WriteNumber(nameof(Duration), Duration);
            writer.WriteBoolean(nameof(CombineModifiers), CombineModifiers);
            writer.WriteNumber(nameof(CombineDecayTime), CombineDecayTime);
            writer.WriteString(nameof(StackType), StackType.ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            writer.WriteNumber(nameof(DebuffID), DebuffID);
            writer.WriteNumber(nameof(GlobalID), GlobalID);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "modstattype":
                case "stattype":
                case "modstat":
                case "stat":
                    StatType = reader.GetString().ToEnum(StatType.Damage);
                    break;
                case "moddamagetype":
                case "damagetype":
                    DamageType = reader.GetString().ToDamageTypes();
                    break;
            }
        }
    }
}
