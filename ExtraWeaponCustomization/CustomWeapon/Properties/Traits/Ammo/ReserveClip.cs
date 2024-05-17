using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using Gear;
using Player;
using System;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class ReserveClip :
        IWeaponProperty<WeaponPostAmmoPackContext>,
        IWeaponProperty<WeaponPreAmmoUIContext>,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponPostFireContext>
    {
        public readonly static string Name = typeof(ReserveClip).Name;
        public bool AllowStack { get; } = false;

        public void Invoke(WeaponPostStartFireContext context)
        {
            if (context.Weapon.ArchetypeData.FireMode != eWeaponFireMode.Burst) return;
            BWA_Burst archetype = context.Weapon.m_archeType.TryCast<BWA_Burst>()!;

            int bullets = context.Weapon.GetCurrentClip() + PlayerBackpackManager.GetBulletsInPack(context.Weapon.AmmoType, context.Weapon.Owner.Owner);
            archetype.m_burstCurrentCount = Math.Min(archetype.m_burstMax, bullets);
        }

        public void Invoke(WeaponPostAmmoPackContext context)
        {
            context.Weapon.Owner.Inventory.DoReload();
        }

        public void Invoke(WeaponPostFireContext context)
        {
            context.Weapon.Owner.Inventory.DoReload();
        }

        public void Invoke(WeaponPreAmmoUIContext context)
        {
            context.Reserve += context.Clip;
            context.ShowClip = false;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader) {}
    }
}
