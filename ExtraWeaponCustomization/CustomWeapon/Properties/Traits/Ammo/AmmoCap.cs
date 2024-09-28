using EWC.CustomWeapon.WeaponContext.Contexts;
using GameData;
using Player;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public class AmmoCap :
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponSetupContext>,
        IWeaponProperty<WeaponPostAmmoInitContext>,
        IWeaponProperty<WeaponPreAmmoPackContext>
    {
        public float AmmoCapRel { get; set; } = 1f;
        public float AmmopackRefillRel { get; set; } = 0f;
        public float CostOfBullet { get; set; } = 0f;

        private const float DefaultPackConv = 5f;

        public void Invoke(WeaponSetupContext context)
        {
            if (AmmopackRefillRel > 0)
            {
                PlayerDataBlock data = GameDataBlockBase<PlayerDataBlock>.GetBlock(1u);
                float capToPack = 1f;
                switch (CWC.Weapon.AmmoType)
                {
                    case AmmoType.Standard:
                        capToPack = (float)data.AmmoStandardMaxCap / data.AmmoStandardResourcePackMaxCap;
                        break;
                    case AmmoType.Special:
                        capToPack = (float)data.AmmoSpecialMaxCap / data.AmmoSpecialResourcePackMaxCap;
                        break;
                }
                AmmoCapRel = AmmopackRefillRel * DefaultPackConv * capToPack;
                AmmopackRefillRel = 0;
            }

            if (CostOfBullet > 0)
            {
                AmmoCapRel = CWC.Weapon.ArchetypeData.CostOfBullet / CostOfBullet;
                CostOfBullet = 0;
            }
        }

        public void Invoke(WeaponPostAmmoInitContext context)
        {
            // Fix the starting ammo for the weapon.
            InventorySlotAmmo slot = context.SlotAmmo;
            slot.AmmoInPack = (CWC.Weapon.GetCurrentClip() * slot.CostOfBullet + slot.AmmoInPack) * AmmoCapRel;
            CWC.Weapon.SetCurrentClip(context.AmmoStorage.GetClipBulletsFromPack(0, slot.AmmoType));
        }

        public void Invoke(WeaponPreAmmoPackContext context)
        {
            // Uses a relative change on the incoming value to handle any more than just ammopack case.
            // Ammopacks are not the only expected change (e.g. XP ammo refill, CConsole).
            context.AmmoRel *= AmmoCapRel;
        }

        public override IWeaponProperty Clone()
        {
            AmmoCap copy = new()
            {
                AmmoCapRel = AmmoCapRel,
                AmmopackRefillRel = AmmopackRefillRel,
                CostOfBullet = CostOfBullet
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", nameof(AmmoCap));
            writer.WriteNumber(nameof(AmmoCapRel), AmmoCapRel);
            writer.WriteNumber(nameof(AmmopackRefillRel), AmmopackRefillRel);
            writer.WriteNumber(nameof(CostOfBullet), CostOfBullet);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
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
