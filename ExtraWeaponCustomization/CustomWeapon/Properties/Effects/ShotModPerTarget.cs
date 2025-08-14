using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.ObjectWrappers;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class ShotModPerTarget : 
        TriggerMod,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponStatContext>,
        IWeaponProperty<WeaponShotGroupInitContext>,
        IWeaponProperty<WeaponShotInitContext>
    {
        private readonly Dictionary<BaseDamageableWrapper, TriggerStack> _triggerStacks = new();
        private static BaseDamageableWrapper TempWrapper => BaseDamageableWrapper.SharedInstance;

        public StatType StatType { get; private set; } = StatType.Damage;
        public DamageType[] DamageType { get; private set; } = DamageTypeConst.Any;
        public bool StoreOnGroup { get; private set; } = true;
        public bool CalcWhenHit { get; private set; } = false;

        public ShotModPerTarget()
        {
            Trigger ??= new(ITrigger.GetTrigger(TriggerName.Hit));
            SetValidTriggers(ITrigger.PositionalTriggers);
        }

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponShotGroupInitContext)) return !CalcWhenHit && StoreOnGroup;
            if (contextType == typeof(WeaponShotInitContext)) return !CalcWhenHit && !StoreOnGroup;
            if (contextType == typeof(WeaponStatContext)) return CalcWhenHit;

            return base.ShouldRegister(contextType);
        }

        public override void TriggerReset() => _triggerStacks.Clear();
        public override void TriggerApply(List<TriggerContext> contexts)
        {
            if (contexts.Count > 5)
            {
                Dictionary<BaseDamageableWrapper, (float triggerAmt, ShotInfo info)> triggerDict = new();
                foreach (var context in contexts)
                {
                    var hitContext = (WeaponHitDamageableContextBase)context.context;
                    IDamageable damageable = hitContext.Damageable;
                    if (damageable == null) continue;

                    TempWrapper.Set(damageable);
                    if (!triggerDict.ContainsKey(TempWrapper))
                        triggerDict.Add(new BaseDamageableWrapper(TempWrapper), (0, hitContext.ShotInfo.Orig));

                    var pair = triggerDict[TempWrapper];
                    pair.triggerAmt += context.triggerAmt;
                    triggerDict[TempWrapper] = pair;
                }

                foreach ((BaseDamageableWrapper wrapper, (float triggerAmt, ShotInfo info)) in triggerDict)
                    AddTriggerInstance(wrapper, triggerAmt, info);
            }
            else
            {
                foreach (var context in contexts)
                {
                    var hitContext = (WeaponHitDamageableContextBase)context.context;
                    IDamageable damageable = hitContext.Damageable;
                    if (damageable == null) continue;

                    AddTriggerInstance(
                        new BaseDamageableWrapper(damageable),
                        context.triggerAmt,
                        hitContext.ShotInfo.Orig
                        );
                }
            }
        }

        private void AddTriggerInstance(BaseDamageableWrapper wrapper, float triggerAmt, ShotInfo shotInfo)
        {
            if (!_triggerStacks.ContainsKey(wrapper))
            {
                // Clean dead agents from dict. Doesn't need to happen here, but we don't need to run this often, so eh
                _triggerStacks.Keys
                    .Where(wrapper => !wrapper.Alive)
                    .ToList()
                    .ForEach(wrapper => _triggerStacks.Remove(wrapper));

                _triggerStacks[wrapper] = new TriggerStack(this);
            }

            if (!CalcWhenHit)
            {
                float mod = CalculateMod(triggerAmt);
                if (StoreOnGroup)
                    shotInfo.GroupMod.Add(this, StatType, mod, wrapper.Object, DamageType);
                else
                    shotInfo.Mod.Add(this, StatType, mod, wrapper.Object, DamageType);
            }

            _triggerStacks[wrapper].Add(triggerAmt);
        }

        public void Invoke(WeaponShotGroupInitContext context)
        {
            foreach ((var damageable, var stack) in _triggerStacks)
            {
                if (stack.TryGetMod(out float mod))
                    context.GroupMod.Add(this, StatType, mod, damageable.Object, DamageType);
            }
        }

        public void Invoke(WeaponShotInitContext context)
        {
            foreach ((var damageable, var stack) in _triggerStacks)
            {
                if (stack.TryGetMod(out float mod))
                    context.Mod.Add(this, StatType, mod, damageable.Object, DamageType);
            }
        }

        public void Invoke(WeaponStatContext context)
        {
            if (!context.DamageType.HasFlagIn(DamageType)) return;

            TempWrapper.Set(context.Damageable);
            if (!_triggerStacks.TryGetValue(TempWrapper, out TriggerStack? triggerStack)) return;
            if (!triggerStack.TryGetMod(out float mod)) return;

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
