using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class HoldBurst :
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponFireCancelContext>
    {
        public int ShotsUntilCancel { get; private set; } = 1;

        private int _burstMaxCount = 0;

        public void Invoke(WeaponPostStartFireContext context)
        {
            if (!CWC.TryGetBurstArchetype(out var arch)) return;
            // Can't use archetype.m_burstMax in case clip < max burst count
            _burstMaxCount = arch.m_burstCurrentCount;
        }

        public void Invoke(WeaponFireCancelContext context)
        {
            if (!CWC.TryGetBurstArchetype(out var arch)) return;

            if (_burstMaxCount - arch.m_burstCurrentCount >= ShotsUntilCancel && !arch.m_fireHeld)
            {
                arch.m_burstCurrentCount = 0;
                context.Allow = false;
            }
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ShotsUntilCancel), ShotsUntilCancel);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "shotsuntilcancel":
                case "shots":
                    ShotsUntilCancel = reader.GetInt32();
                    break;
                default:
                    break;
            }
        }
    }
}
