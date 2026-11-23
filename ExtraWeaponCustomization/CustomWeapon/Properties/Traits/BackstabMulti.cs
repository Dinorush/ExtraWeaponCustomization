using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class BackstabMulti : 
        Trait,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>,
        IWeaponProperty<WeaponStatContext>
    {
        public float BackstabDamageMulti { get; private set; } = 1f;

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponSetupContext)
             || contextType == typeof(WeaponClearContext))
                return CWC.Weapon.IsType(WeaponType.Sentry);
            return base.ShouldRegister(contextType);
        }
        public void Invoke(WeaponSetupContext _)
        {
            ((SentryGunComp)CWC.Weapon).AllowBackstabOverride = true;
        }

        public void Invoke(WeaponClearContext _)
        {
            ((SentryGunComp)CWC.Weapon).AllowBackstabOverride = false;
        }

        public void Invoke(WeaponStatContext context)
        {
            context.AddMod(StatType.Backstab, BackstabDamageMulti, StackType.Multiply);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(BackstabDamageMulti), BackstabDamageMulti);
            writer.WriteEndObject();
        }

        public override bool DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            if (base.DeserializeProperty(property, ref reader))
                return true;

            switch (property.ToLowerInvariant())
            {
                case "backstabdamagemulti":
                case "backdamagemulti":
                case "backstabmulti":
                case "backmulti":
                case "multi":
                    BackstabDamageMulti = reader.GetSingle();
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
