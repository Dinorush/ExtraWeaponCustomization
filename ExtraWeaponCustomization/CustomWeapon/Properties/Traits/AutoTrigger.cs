using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class AutoTrigger : 
        Trait,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        private bool _cachedTrigger;

        protected override OwnerType RequiredOwnerType => OwnerType.Local;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public void Invoke(WeaponSetupContext context)
        {
            var gun = (LocalGunComp)CWC.Weapon;
            _cachedTrigger = gun.GunArchetype.m_triggerNeedsPress;
            gun.GunArchetype.m_triggerNeedsPress = false;
        }

        public void Invoke(WeaponClearContext context)
        {
            ((LocalGunComp)CWC.Weapon).GunArchetype.m_triggerNeedsPress = _cachedTrigger;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }
    }
}
