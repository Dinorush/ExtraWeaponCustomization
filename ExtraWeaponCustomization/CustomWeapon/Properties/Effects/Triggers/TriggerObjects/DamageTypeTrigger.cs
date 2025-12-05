using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class DamageTypeTrigger<TContext> : IDamageTypeTrigger where TContext : WeaponDamageTypeContext
    {
        public DamageType[] DamageTypes { get; }
        public DamageType BlacklistType { get; set; } = DamageType.Any;
        public TriggerName Name { get; private set; }
        public float Amount { get; private set; } = 1f;
        public uint MaxPerShot { get; private set; } = 0;

        public DamageTypeTrigger(TriggerName name, params DamageType[] types)
        {
            Name = name;
            DamageTypes = types.Length == 0 ? DamageTypeConst.Any : types;
        }

        public virtual bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;
            if (context is TContext hitContext
                && !hitContext.DamageType.HasAnyFlag(BlacklistType)
                && hitContext.DamageType.HasFlagIn(DamageTypes)
                && (MaxPerShot == 0 || MaxPerShot > hitContext.ShotInfo.TypeHits(DamageTypes, BlacklistType)))
            {
                amount = Amount;
                return true;
            }
            return false;
        }

        public virtual void Reset() { }

        protected virtual bool CloneObject => false;

        public virtual ITrigger Clone()
        {
            if (!CloneObject) return this;

            Type type = GetType();
            ITrigger copy = type.GetConstructor(new Type[] { typeof(DamageType[]) }) != null ? CopyUtil.Clone(this, DamageTypes) : CopyUtil.Clone(this, Name, DamageTypes);

            var typeTrigger = (DamageTypeTrigger<TContext>)copy;
            typeTrigger.BlacklistType = BlacklistType;

            return copy;
        }

        public virtual void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "triggeramount":
                case "amount":
                    Amount = reader.GetSingle();
                    break;
                case "maxpershot":
                    MaxPerShot = reader.GetUInt32();
                    break;
            }
        }
    }
}
