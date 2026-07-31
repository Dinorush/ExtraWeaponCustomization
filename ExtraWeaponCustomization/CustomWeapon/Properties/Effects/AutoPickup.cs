using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Pickup;
using EWC.CustomWeapon.Properties.Shared.Triggers;
using System.Collections.Generic;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class AutoPickup :
        Effect
    {
        protected override WeaponType ValidWeaponType => WeaponType.Sentry | WeaponType.SentryHolder;

        public override bool ValidProperty()
        {
            if (CWC.Owner.Player == null) return false;
            return base.ValidProperty();
        }

        public override void TriggerApply(List<TriggerContext> contexts)
        {
            PickupManager.DoPickup(CWC);
        }

        public override void TriggerReset() { }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }
    }
}
