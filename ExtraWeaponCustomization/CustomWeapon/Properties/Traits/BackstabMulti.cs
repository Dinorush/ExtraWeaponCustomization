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
        IWeaponProperty<WeaponBackstabContext>
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

        public void Invoke(WeaponBackstabContext context)
        {
            context.AddMod(BackstabDamageMulti, StackType.Multiply);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(BackstabDamageMulti), BackstabDamageMulti);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property.ToLowerInvariant())
            {
                case "backstabdamagemulti":
                case "backdamagemulti":
                case "backstabmulti":
                case "backmulti":
                case "multi":
                    BackstabDamageMulti = reader.GetSingle();
                    break;
            }
        }
    }
}
