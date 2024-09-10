using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class HoldBurst :
        Trait,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponFireCancelContext>
    {
        public int ShotsUntilCancel = 1;

        private int _burstMaxCount = 0;

        public void Invoke(WeaponPostStartFireContext context)
        {
            if (context.Weapon.ArchetypeData.FireMode != eWeaponFireMode.Burst) return;
            BWA_Burst archetype = context.Weapon.m_archeType.TryCast<BWA_Burst>()!;
            // Can't use archetype.m_burstMax in case clip < max burst count
            _burstMaxCount = archetype.m_burstCurrentCount;
        }

        public void Invoke(WeaponFireCancelContext context)
        {
            if (context.Weapon.ArchetypeData.FireMode != eWeaponFireMode.Burst) return;
            BWA_Burst archetype = context.Weapon.m_archeType.TryCast<BWA_Burst>()!;

            if (_burstMaxCount - archetype.m_burstCurrentCount >= ShotsUntilCancel && !archetype.m_fireHeld)
            {
                archetype.m_burstCurrentCount = 0;
                context.Allow = false;
            }
        }

        public override IWeaponProperty Clone()
        {
            HoldBurst copy = new()
            {
                ShotsUntilCancel = ShotsUntilCancel
            };
            return copy;
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
