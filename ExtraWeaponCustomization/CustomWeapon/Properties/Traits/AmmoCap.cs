﻿using EWC.CustomWeapon.WeaponContext.Contexts;
using GameData;
using Player;
using System;
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
        public float AmmoCapRel { get; private set; } = 1f;
        public float AmmopackRefillRel { get; private set; } = 0f;
        public float CostOfBullet { get; private set; } = 0f;
        public bool ApplyOnDrop { get; private set; } = true;

        private const float DefaultPackConv = 5f;

        public override bool ShouldRegister(Type contextType)
        {
            if (contextType == typeof(WeaponPostAmmoInitContext)) return ApplyOnDrop;
            return base.ShouldRegister(contextType);
        }

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
                AmmoCapRel = CWC.ArchetypeData.CostOfBullet / CostOfBullet;
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
            context.AmmoAmount *= AmmoCapRel;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", nameof(AmmoCap));
            writer.WriteNumber(nameof(AmmoCapRel), AmmoCapRel);
            writer.WriteNumber(nameof(AmmopackRefillRel), AmmopackRefillRel);
            writer.WriteNumber(nameof(CostOfBullet), CostOfBullet);
            writer.WriteBoolean(nameof(ApplyOnDrop), ApplyOnDrop);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
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
                    float cap = reader.GetSingle();
                    if (cap == 1f) break;
                    AmmoCapRel = cap;
                    CostOfBullet = 0;
                    AmmopackRefillRel = 0;
                    break;
                case "ammopackrefillrel":
                case "ammopackrefill":
                case "ammorefillrel":
                case "ammorefill":
                case "refillrel":
                case "refill":
                    AmmopackRefillRel = reader.GetSingle();
                    CostOfBullet = 0;
                    break;
                case "applyondrop":
                    ApplyOnDrop = reader.GetBoolean();
                    break;
                default:
                    break;
            }
        }
    }
}
