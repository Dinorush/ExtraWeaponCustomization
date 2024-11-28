using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects.Triggers
{
    public class DamageTypeTrigger<TContext> : IDamageTypeTrigger where TContext : WeaponDamageTypeContext
    {
        public DamageType DamageType { get; private set; }
        public DamageType BlacklistType { get; set; } = DamageType.Any;
        public TriggerName Name { get; private set; }
        public float Amount { get; private set; } = 1f;

        private static readonly Dictionary<Type, List<PropertyInfo>> _classProperties = new();

        public DamageTypeTrigger(TriggerName name, DamageType type = DamageType.Any)
        {
            Name = name;
            DamageType = type;
        }

        public virtual bool Invoke(WeaponTriggerContext context, out float amount)
        {
            amount = 0f;

            if (context is TContext hitContext
                && !hitContext.DamageType.HasAnyFlag(BlacklistType)
                && hitContext.DamageType.HasFlag(DamageType))
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
            if (!_classProperties.ContainsKey(type))
            {
                List<PropertyInfo> properties = new();
                _classProperties.Add(type, properties);

                for (Type currentType = type; currentType.BaseType != null; currentType = currentType.BaseType)
                {
                    foreach (var prop in currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        MethodInfo? mget = prop.GetGetMethod(false);
                        MethodInfo? mset = prop.GetSetMethod(true);

                        // Only want properties with public get and private set
                        if (mget == null || mset == null || mset.IsPublic || !prop.CanWrite) continue;

                        properties.Add(prop);
                    }
                }
            }

            ITrigger copy = (ITrigger) (type.GetConstructor(new Type[] { typeof(DamageType) }) != null ? Activator.CreateInstance(type, DamageType) : Activator.CreateInstance(type, Name, DamageType))!;
            foreach (var prop in _classProperties[type])
                prop.SetValue(copy, prop.GetValue(this));

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
            }
        }
    }
}
