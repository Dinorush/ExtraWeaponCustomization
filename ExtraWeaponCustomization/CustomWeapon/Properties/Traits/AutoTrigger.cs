using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class AutoTrigger : 
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponClearContext>
    {
        private bool _cachedTrigger;

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponSetupContext) || contextType == typeof(WeaponClearContext)) return CWC.IsLocal;
            return base.ShouldRegister(contextType);
        }

        public void Invoke(WeaponSetupContext context)
        {
            _cachedTrigger = CWC.Gun!.m_archeType.m_triggerNeedsPress;
            CWC.Gun.m_archeType.m_triggerNeedsPress = false;
        }

        public void Invoke(WeaponClearContext context)
        {
            CWC.Gun!.m_archeType.m_triggerNeedsPress = _cachedTrigger;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }
    }
}
