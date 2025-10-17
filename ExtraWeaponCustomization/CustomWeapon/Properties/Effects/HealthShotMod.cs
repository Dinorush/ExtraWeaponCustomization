using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class HealthShotMod :
        Effect,
        IWeaponProperty<WeaponCreatedContext>,
        IWeaponProperty<WeaponStatContext>,
        IWeaponProperty<WeaponShotInitContext>,
        IWeaponProperty<WeaponShotGroupInitContext>
    {
        public float ModAtMin { get; private set; } = 1f;
        public float ModAtMax { get; private set; } = 1f;
        public float HealthMinRel { get; private set; } = 0f;
        public float HealthMaxRel { get; private set; } = 1f;
        public float Exponent { get; private set; } = 1f;
        public bool FlipExponent { get; private set; } = false;
        public StatType StatType { get; private set; } = StatType.Damage;
        public StackType StackLayer { get; private set; } = StackType.Multiply;
        public DamageType[] DamageType { get; private set; } = DamageTypeConst.Any;
        public bool StoreOnGroup { get; private set; } = true;
        public bool CalcWhenHit { get; private set; } = false;

        private Dam_PlayerDamageBase _damBase = null!;

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponShotGroupInitContext)) return !CalcWhenHit && StoreOnGroup;
            if (contextType == typeof(WeaponShotInitContext)) return !CalcWhenHit && !StoreOnGroup;
            if (contextType == typeof(WeaponStatContext)) return CalcWhenHit;

            return base.ShouldRegister(contextType);
        }

        public override void TriggerReset() { }
        public override void TriggerApply(List<TriggerContext> contexts) { }

        public void Invoke(WeaponCreatedContext context)
        {
            _damBase = CWC.Owner.Player.Damage;
        }

        public void Invoke(WeaponShotInitContext context)
        {
            context.Mod.Add(this, StatType, GetMod(), 0f, StackType.Override, StackLayer, null, DamageType);
        }

        public void Invoke(WeaponShotGroupInitContext context)
        {
            context.GroupMod.Add(this, StatType, GetMod(), 0f, StackType.Override, StackLayer, null, DamageType);
        }

        public void Invoke(WeaponStatContext context)
        {
            context.AddMod(StatType, GetMod(), StackLayer);
        }

        private float GetMod()
        {
            if (FlipExponent)
                return (_damBase.Health / _damBase.HealthMax).MapInverted(HealthMinRel, HealthMaxRel, ModAtMin, ModAtMax, Exponent);
            return (_damBase.Health / _damBase.HealthMax).Map(HealthMinRel, HealthMaxRel, ModAtMin, ModAtMax, Exponent);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ModAtMin), ModAtMin);
            writer.WriteNumber(nameof(ModAtMax), ModAtMax);
            writer.WriteNumber(nameof(HealthMinRel), HealthMinRel);
            writer.WriteNumber(nameof(HealthMaxRel), HealthMaxRel);
            writer.WriteNumber(nameof(Exponent), Exponent);
            writer.WriteBoolean(nameof(FlipExponent), FlipExponent);
            writer.WriteString(nameof(StatType), StatType.ToString());
            writer.WriteString(nameof(DamageType), DamageType[0].ToString());
            writer.WriteString(nameof(StackLayer), StackLayer.ToString());
            writer.WriteBoolean(nameof(StoreOnGroup), StoreOnGroup);
            writer.WriteBoolean(nameof(CalcWhenHit), CalcWhenHit);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "modatmin":
                    ModAtMin = reader.GetSingle();
                    break;
                case "modatmax":
                    ModAtMax = reader.GetSingle();
                    break;
                case "healthminrel":
                case "healthmin":
                    HealthMinRel = reader.GetSingle();
                    break;
                case "healthmaxrel":
                case "healthmax":
                    HealthMaxRel = reader.GetSingle();
                    break;
                case "exponent":
                    Exponent = reader.GetSingle();
                    break;
                case "flipexponent":
                case "flip":
                    FlipExponent = reader.GetBoolean();
                    break;
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
                case "stacklayer":
                case "layer":
                    StackLayer = reader.GetString().ToEnum(StackType.Invalid);
                    break;
                case "storemodongroup":
                case "storeongroup":
                    StoreOnGroup = reader.GetBoolean();
                    break;
                case "calcwhenhit":
                    CalcWhenHit = reader.GetBoolean();
                    break;
            }
        }
    }
}
