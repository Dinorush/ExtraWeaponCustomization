using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ShotMod :
        TriggerMod,
        IWeaponProperty<WeaponStatContext>,
        IWeaponProperty<WeaponShotInitContext>,
        IWeaponProperty<WeaponShotGroupInitContext>
    {
        private readonly TriggerStack _triggerStack;

        public StatType StatType { get; private set; } = StatType.Damage;
        public DamageType[] DamageType { get; private set; } = DamageTypeConst.Any;
        public bool StoreOnGroup { get; private set; } = true;
        public bool CalcWhenHit { get; private set; } = false;

        public ShotMod() => _triggerStack = new(this);

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponShotGroupInitContext)) return !CalcWhenHit && StoreOnGroup;
            if (contextType == typeof(WeaponShotInitContext)) return !CalcWhenHit && !StoreOnGroup;
            if (contextType == typeof(WeaponStatContext)) return CalcWhenHit;

            return base.ShouldRegister(contextType);
        }

        public override bool TryGetStacks(out float stacks, BaseDamageableWrapper? _ = null) => _triggerStack.TryGetStacks(out stacks);

        public override void TriggerReset() => _triggerStack.Clear();
        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (!CalcWhenHit)
            {
                float mod = CalculateMod(Count(contexts));
                foreach (var tContext in contexts)
                {
                    if (tContext.context is WeaponHitContextBase hitContext)
                    {
                        if (StoreOnGroup)
                            hitContext.ShotInfo.Orig.GroupMod.Add(this, StatType, mod, null, DamageType);
                        else
                            hitContext.ShotInfo.Orig.Mod.Add(this, StatType, mod, null, DamageType);
                    }
                }
            }

            _triggerStack.Add(contexts);
        }

        public void Invoke(WeaponShotInitContext context)
        {
            if (!_triggerStack.TryGetMod(out float mod)) return;

            context.Mod.Add(this, StatType, mod, null, DamageType);
        }

        public void Invoke(WeaponShotGroupInitContext context)
        {
            if (!_triggerStack.TryGetMod(out float mod)) return;

            context.GroupMod.Add(this, StatType, mod, null, DamageType);
        }

        public void Invoke(WeaponStatContext context)
        {
            if (!context.DamageType.HasFlagIn(DamageType) || !_triggerStack.TryGetMod(out float mod)) return;

            context.AddMod(StatType, mod, StackLayer);
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
            writer.WriteBoolean(nameof(StoreOnGroup), StoreOnGroup);
            writer.WriteBoolean(nameof(CalcWhenHit), CalcWhenHit);
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
