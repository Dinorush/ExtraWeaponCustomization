using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class EMPImmunity : 
        Trait,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        protected override OwnerType RequiredOwnerType => OwnerType.Managed;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        private MonoBehaviour _component = null!;

        public override bool ValidProperty()
        {
            if (!EECAPIWrapper.TryGetEMPController(CWC, out _component!))
                return false;
            return base.ValidProperty();
        }

        public void Invoke(WeaponSetupContext context)
        {
            EECAPIWrapper.RemoveEMPController(_component);
        }

        public void Invoke(WeaponClearContext context)
        {
            if (!CWC.Destroyed)
                EECAPIWrapper.AddEMPController(_component);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }
    }
}
