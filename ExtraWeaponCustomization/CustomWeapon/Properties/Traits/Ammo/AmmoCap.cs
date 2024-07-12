using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using GameData;
using Player;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public class AmmoCap :
        IWeaponProperty<WeaponPostSetupContext>,
        IWeaponProperty<WeaponPostAmmoInitContext>,
        IWeaponProperty<WeaponPreAmmoPackContext>
    {
        public bool AllowStack { get; } = false;

        public float AmmoCapRel { get; set; } = 1f;
        public float AmmopackRefillRel { get; set; } = 0f;
        public float CostOfBullet { get; set; } = 0f;

        private const float DefaultPackConv = 5f;

        public void Invoke(WeaponPostSetupContext context)
        {
            if (AmmopackRefillRel > 0)
            {
                PlayerDataBlock data = GameDataBlockBase<PlayerDataBlock>.GetBlock(1u);
                float capToPack = 1f;
                switch (context.Weapon.AmmoType)
                {
                    case AmmoType.Standard:
                        capToPack = (float)data.AmmoStandardMaxCap / data.AmmoStandardResourcePackMaxCap;
                        break;
                    case AmmoType.Special:
                        capToPack = (float)data.AmmoSpecialMaxCap / data.AmmoSpecialResourcePackMaxCap;
                        break;
                }
                AmmoCapRel = AmmopackRefillRel * DefaultPackConv * capToPack;
            }

            if (CostOfBullet > 0)
                AmmoCapRel = context.Weapon.ArchetypeData.CostOfBullet / CostOfBullet;
        }

        public void Invoke(WeaponPostAmmoInitContext context)
        {
            // Fix the starting ammo for the weapon.
            InventorySlotAmmo slot = context.SlotAmmo;
            slot.AmmoInPack = (context.Weapon.GetCurrentClip() * slot.CostOfBullet + slot.AmmoInPack) * AmmoCapRel;
            context.Weapon.SetCurrentClip(context.AmmoStorage.GetClipBulletsFromPack(0, slot.AmmoType));
        }

        public void Invoke(WeaponPreAmmoPackContext context)
        {
            // Uses a relative change on the incoming value to handle any more than just ammopack case.
            // Ammopacks are not the only expected change (e.g. XP ammo refill, CConsole).
            context.AmmoRel *= AmmoCapRel;
        }

        public IWeaponProperty Clone()
        {
            AmmoCap copy = new()
            {
                AmmoCapRel = AmmoCapRel,
                AmmopackRefillRel = AmmopackRefillRel,
                CostOfBullet = CostOfBullet
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", nameof(AmmoCap));
            writer.WriteNumber(nameof(AmmoCapRel), AmmoCapRel);
            writer.WriteNumber(nameof(AmmopackRefillRel), AmmopackRefillRel);
            writer.WriteNumber(nameof(CostOfBullet), CostOfBullet);
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            switch (property)
            {
                case "costofbullet":
                    CostOfBullet = reader.GetSingle();
                    AmmopackRefillRel = 0;
                    break;
                case "ammocaprel":
                case "ammocap":
                case "caprel":
                case "cap":
                    AmmoCapRel = reader.GetSingle();
                    CostOfBullet = 0;
                    AmmopackRefillRel = 0;
                    break;
                case "ammorefillrel":
                case "ammorefill":
                case "refillrel":
                case "refill":
                    AmmopackRefillRel = reader.GetSingle();
                    CostOfBullet = 0;
                    break;
                default:
                    break;
            }
        }
    }
}
