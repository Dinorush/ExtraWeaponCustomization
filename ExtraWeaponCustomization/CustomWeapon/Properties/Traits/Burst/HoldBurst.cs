using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using Gear;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class HoldBurst : IWeaponProperty<WeaponPreFireContext>, IWeaponProperty<WeaponPostStopFiringContext>
    {
        public readonly static string Name = typeof(HoldBurst).Name;
        public bool AllowStack { get; } = false;

        public int ShotsUntilCancel = 1;

        private int _burstMaxCount = 0;

        public void Invoke(WeaponPreFireContext context)
        {
            if (context.Weapon.ArchetypeData.FireMode != eWeaponFireMode.Burst) return;
            BWA_Burst archetype = context.Weapon.m_archeType.TryCast<BWA_Burst>()!;

            if (_burstMaxCount == 0) // Can't use archetype.m_burstMax in case clip < max burst count
                _burstMaxCount = archetype.m_burstCurrentCount;
            if (_burstMaxCount - archetype.m_burstCurrentCount >= ShotsUntilCancel && !archetype.m_fireHeld)
            {
                archetype.m_burstCurrentCount = 0;
                context.Allow = false;
            }
        }

        public void Invoke(WeaponPostStopFiringContext context)
        {
            _burstMaxCount = 0;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(ShotsUntilCancel), ShotsUntilCancel);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
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
