using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using System;
using Player;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class AmmoMod : IWeaponProperty<WeaponTriggerContext>
    {
        public readonly static string Name = typeof(AmmoMod).Name;
        public bool AllowStack { get; } = true;

        public int ClipChange { get; set; } = 0;
        public int ReserveChange { get; set; } = 0;
        public bool PullFromReserve { get; set; } = false;
        public TriggerType TriggerType { get; set; } = TriggerType.Invalid;

        public void Invoke(WeaponTriggerContext context)
        {
            if (context.Type != TriggerType) return;

            // Calculate the actual changes we can make to clip/ammo
            PlayerAmmoStorage ammoStorage = PlayerBackpackManager.GetBackpack(context.Weapon.Owner.Owner).AmmoStorage;
            int clipChange = PullFromReserve ? Math.Min(ClipChange, ammoStorage.GetBulletsInPack(context.Weapon.AmmoType)) : ClipChange;
            int newClip = Math.Clamp(context.Weapon.GetCurrentClip() + clipChange, 0, context.Weapon.GetMaxClip() + 1);
            clipChange = newClip - context.Weapon.GetCurrentClip();
            int reserveChange = PullFromReserve ? ReserveChange - clipChange : ReserveChange;

            context.Weapon.SetCurrentClip(newClip);

            // Need to update UI since UpdateBulletsInPack does it without including the clip
            ammoStorage.UpdateBulletsInPack(context.Weapon.AmmoType, reserveChange);
            ammoStorage.UpdateSlotAmmoUI(ammoStorage.m_ammoStorage[(int) context.Weapon.AmmoType], newClip);
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(nameof(Name), Name);
            writer.WriteNumber(nameof(ClipChange), ClipChange);
            writer.WriteNumber(nameof(ReserveChange), ReserveChange);
            writer.WriteBoolean(nameof(PullFromReserve), PullFromReserve);
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property.ToLowerInvariant())
            {
                case "clipchange":
                case "clip":
                    ClipChange = reader.GetInt32();
                    break;
                case "reservechange":
                case "reserve":
                    ReserveChange = reader.GetInt32();
                    break;
                case "pullfromreserve":
                    PullFromReserve = reader.GetBoolean();
                    break;
                case "triggertype":
                case "trigger":
                    TriggerType = reader.GetString()?.ToTriggerType() ?? TriggerType.Invalid;
                    break;
                default:
                    break;
            }
        }
    }
}
