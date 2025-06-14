using System;
using Player;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;
using System.Collections.Generic;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using System.Linq;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class AmmoMod :
        Effect,
        IGunProperty,
        IMeleeProperty,
        IWeaponProperty<WeaponPreFireContext>,
        IWeaponProperty<WeaponPostFireContext>
    {
        public float ClipChange { get; private set; } = 0;
        public float ReserveChange { get; private set; } = 0;
        public bool OverflowToReserve { get; private set; } = true;
        public bool PullFromReserve { get; private set; } = false;
        public bool UseRawAmmo { get; private set; } = false;
        public bool BypassReserveCap { get; private set; } = false;
        public InventorySlot ReceiverSlot { get; private set; } = InventorySlot.None;

        private float _clipBuffer = 0;
        private float _reserveBuffer = 0;
        private bool _bonusRound = false;

        public void Invoke(WeaponPreFireContext context)
        {
            _bonusRound = SlotMatchesWeapon();
        }

        public void Invoke(WeaponPostFireContext context)
        {
            _bonusRound = false;
        }

        public override bool ValidProperty()
        {
            return base.ValidProperty() && (CWC.IsGun || ReceiverSlot != InventorySlot.None);
        }

        public override void TriggerReset()
        {
            _clipBuffer = 0;
            _reserveBuffer = 0;
        }

        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            PlayerBackpack backpack = PlayerBackpackManager.GetBackpack(CWC.Weapon.Owner.Owner);
            ItemEquippable? weapon = CWC.Gun;
            if (!SlotMatchesWeapon() && backpack.TryGetBackpackItem(ReceiverSlot, out var bpItem) && bpItem.Instance != null)
                weapon = bpItem.Instance.Cast<ItemEquippable>();

            if (weapon == null) return;

            PlayerAmmoStorage ammoStorage = backpack.AmmoStorage;
            InventorySlotAmmo slotAmmo = ammoStorage.GetInventorySlotAmmo(weapon.AmmoType);

            float triggers = triggerList.Sum(context => context.triggerAmt);
            _clipBuffer += ClipChange * triggers;
            _reserveBuffer += ReserveChange * triggers;

            float costOfBullet = slotAmmo.CostOfBullet;
            float min = UseRawAmmo ? costOfBullet : 1f;
            if (Math.Abs(_clipBuffer) < min && Math.Abs(_reserveBuffer) < min) return;
            
            // Ammo decrements after this callback if on kill/shot/hit, need to account for that.
            // But if this weapon didn't get the kill (e.g. DOT kill), shouldn't do that.
            int accountForShot = _bonusRound ? 1 : 0;

            if (UseRawAmmo)
            {
                _clipBuffer /= costOfBullet;
                _reserveBuffer /= costOfBullet;
            }

            // Calculate the actual changes we can make to clip/ammo
            int clipChange = (int) (PullFromReserve ? Math.Min(_clipBuffer, slotAmmo.BulletsInPack) : _clipBuffer);
            int newClip = Math.Clamp(weapon.GetCurrentClip() + clipChange, accountForShot, weapon.GetMaxClip() + accountForShot);

            // If we overflow/underflow the magazine, send the rest to reserves (if not pulling from reserves)
            int bonusReserve = OverflowToReserve ? clipChange - (newClip - weapon.GetCurrentClip()) : 0;
            clipChange = newClip - weapon.GetCurrentClip();

            int reserveChange = (int) (PullFromReserve ? _reserveBuffer - clipChange : _reserveBuffer + bonusReserve);

            _clipBuffer -= (int) _clipBuffer;
            _reserveBuffer -= (int) _reserveBuffer;

            weapon.SetCurrentClip(newClip);

            if (UseRawAmmo)
            {
                _clipBuffer *= costOfBullet;
                _reserveBuffer *= costOfBullet;
            }

            float reserveCost = reserveChange * costOfBullet;
            if (BypassReserveCap || reserveCost < 0)
            {
                slotAmmo.AmmoInPack = Math.Max(slotAmmo.AmmoInPack + reserveCost, 0);
            }
            else if (!slotAmmo.IsFull)
            {
                slotAmmo.AmmoInPack = Math.Clamp(slotAmmo.AmmoInPack + reserveCost, 0, slotAmmo.AmmoMaxCap);
            }

            slotAmmo.OnBulletsUpdateCallback?.Invoke(slotAmmo.BulletsInPack);
            ammoStorage.NeedsSync = true;
            ammoStorage.UpdateSlotAmmoUI(slotAmmo, newClip);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ClipChange), ClipChange);
            writer.WriteNumber(nameof(ReserveChange), ReserveChange);
            writer.WriteBoolean(nameof(OverflowToReserve), OverflowToReserve);
            writer.WriteBoolean(nameof(PullFromReserve), PullFromReserve);
            writer.WriteBoolean(nameof(UseRawAmmo), UseRawAmmo);
            writer.WriteBoolean(nameof(BypassReserveCap), BypassReserveCap);
            writer.WriteString(nameof(ReceiverSlot), SlotToName(ReceiverSlot));
            SerializeTrigger(writer);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
            base.DeserializeProperty(property, ref reader);
            switch (property)
            {
                case "clipchange":
                case "clip":
                    ClipChange = reader.GetSingle();
                    break;
                case "reservechange":
                case "reserve":
                    ReserveChange = reader.GetSingle();
                    break;
                case "overflowtoreserve":
                case "overflow":
                    OverflowToReserve = reader.GetBoolean();
                    break;
                case "pullfromreserve":
                    PullFromReserve = reader.GetBoolean();
                    break;
                case "userawammo":
                case "useammo":
                    UseRawAmmo = reader.GetBoolean();
                    break;
                case "bypassreservecap":
                case "bypasscap":
                    BypassReserveCap = reader.GetBoolean();
                    break;
                case "receiverslot":
                case "slot":
                    ReceiverSlot = SlotNameToSlot(reader.GetString()!);
                    break;
                default:
                    break;
            }
        }

        private static InventorySlot SlotNameToSlot(string name)
        {
            return name.ToLowerInvariant().Replace(" ", null) switch
            {
                "main" or "primary" => InventorySlot.GearStandard,
                "special" or "secondary" => InventorySlot.GearSpecial,
                "tool" or "class" => InventorySlot.GearClass,
                _ => InventorySlot.None
            };
        }

        private bool SlotMatchesWeapon()
        {
            return ReceiverSlot switch
            {
                InventorySlot.GearStandard => CWC.Weapon.AmmoType == AmmoType.Standard,
                InventorySlot.GearSpecial => CWC.Weapon.AmmoType == AmmoType.Special,
                InventorySlot.None => true,
                _ => false,
            };
        }

        private static string SlotToName(InventorySlot slot)
        {
            return slot switch
            {
                InventorySlot.GearStandard => "Main",
                InventorySlot.GearSpecial => "Special",
                InventorySlot.GearClass => "Tool",
                _ => "None",
            };
        }
    }
}
