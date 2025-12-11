using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using FX_EffectSystem;
using GameData;
using Gear;
using Player;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Effects
{
    public sealed class AmmoMod :
        Effect,
        IWeaponProperty<WeaponPreFireContext>,
        IWeaponProperty<WeaponPostFireContext>
    {
        public float ClipChange { get; private set; } = 0;
        public float ReserveChange { get; private set; } = 0;
        public bool OverflowAtBounds { get; private set; } = true;
        public bool PullFromReserve { get; private set; } = false;
        public bool UseRawAmmo { get; private set; } = false;
        public bool BypassClipCap { get; private set; } = false;
        public bool BypassReserveCap { get; private set; } = false;
        public InventorySlot ReceiverSlot { get; private set; } = InventorySlot.None;

        private float _clipBuffer = 0;
        private float _reserveBuffer = 0;
        private bool _bonusRound = false;

        protected override OwnerType RequiredOwnerType => OwnerType.Managed;

        public override bool ValidProperty()
        {
            return base.ValidProperty() && (!CWC.Weapon.IsType(WeaponType.Melee) || ReceiverSlot != InventorySlot.None);
        }

        public void Invoke(WeaponPreFireContext context)
        {
            _bonusRound = SlotMatchesWeapon();
        }

        public void Invoke(WeaponPostFireContext context)
        {
            _bonusRound = false;
        }

        public override void TriggerReset()
        {
            _clipBuffer = 0;
            _reserveBuffer = 0;
        }

        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            if (HandleLevelSentry(triggerList)) return;

            PlayerBackpack? backpack = PlayerBackpackManager.GetBackpack(CWC.Owner.Player!.Owner);
            IAmmoComp weapon;
            if (!SlotMatchesWeapon() && backpack.TryGetBackpackItem(ReceiverSlot, out var bpItem))
            {
                if (!TryCreateHolder(bpItem, out weapon!))
                    return;
            }
            else if (CWC.Weapon.IsType(WeaponType.Gun))
                weapon = CGC.Gun;
            else if (CWC.Weapon.IsType(WeaponType.SentryHolder) && !CustomWeaponManager.TryGetSentry(CWC.Owner.Player, out _))
                weapon = (IAmmoComp)CWC.Weapon;
            else
                return;

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

            int reserves = slotAmmo.BulletsInPack;
            int clipMod = 0;
            if (OverflowAtBounds && ReserveChange < 0 && reserves < -_reserveBuffer)
            {
                int remainder = (int)_reserveBuffer + reserves;
                _reserveBuffer = -reserves;
                clipMod = remainder;
            }

            int maxClip = weapon.GetMaxClip(out bool overflow);
            if (BypassClipCap)
                maxClip = int.MaxValue - accountForShot;
            else if (overflow)
                accountForShot = 0;

            // Calculate the actual changes we can make to clip/ammo
            int clipChange = (int) (PullFromReserve ? Math.Min(_clipBuffer, reserves) : _clipBuffer + clipMod);
            int newClip = Math.Clamp(weapon.GetCurrentClip() + clipChange, accountForShot, maxClip + accountForShot);

            // If we overflow/underflow the magazine, send the rest to reserves (if not pulling from reserves)
            int bonusReserve = OverflowAtBounds ? clipChange - (newClip - weapon.GetCurrentClip()) : 0;
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

        private bool HandleLevelSentry(List<TriggerContext> triggerList)
        {
            if (!CWC.Weapon.IsType(WeaponType.Sentry) || CWC.Owner.Player != null) return false;

            SentryGunComp sentry = (SentryGunComp)CWC.Weapon;
            float triggers = triggerList.Sum(context => context.triggerAmt);
            _clipBuffer += ClipChange * triggers;

            float costOfBullet = sentry.ArchetypeData.CostOfBullet;
            float min = UseRawAmmo ? costOfBullet : 1f;
            if (Math.Abs(_clipBuffer) < min) return true;

            // Ammo decrements after this callback if on kill/shot/hit, need to account for that.
            // But if this weapon didn't get the kill (e.g. DOT kill), shouldn't do that.
            int accountForShot = _bonusRound ? 1 : 0;

            if (UseRawAmmo)
                _clipBuffer /= costOfBullet;

            int maxClip = sentry.GetMaxClip(out bool overflow);
            if (BypassClipCap)
                maxClip = int.MaxValue - accountForShot;
            else if (overflow)
                accountForShot = 0;

            // Calculate the actual changes we can make to clip/ammo
            int newClip = Math.Clamp(sentry.GetCurrentClip() + (int)_clipBuffer, accountForShot, maxClip + accountForShot);
            _clipBuffer -= (int)_clipBuffer;
            sentry.SetCurrentClip(newClip);

            if (UseRawAmmo)
                _clipBuffer *= costOfBullet;

            return true;
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ClipChange), ClipChange);
            writer.WriteNumber(nameof(ReserveChange), ReserveChange);
            writer.WriteBoolean(nameof(OverflowAtBounds), OverflowAtBounds);
            writer.WriteBoolean(nameof(PullFromReserve), PullFromReserve);
            writer.WriteBoolean(nameof(UseRawAmmo), UseRawAmmo);
            writer.WriteBoolean(nameof(BypassClipCap), BypassClipCap);
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
                case "overflowatbounds":
                case "overflowtoreserve":
                case "overflow":
                    OverflowAtBounds = reader.GetBoolean();
                    break;
                case "pullfromreserve":
                    PullFromReserve = reader.GetBoolean();
                    break;
                case "userawammo":
                case "useammo":
                    UseRawAmmo = reader.GetBoolean();
                    break;
                case "bypassclipcap":
                    BypassClipCap = reader.GetBoolean();
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
            switch (ReceiverSlot)
            {
                case InventorySlot.GearStandard:
                case InventorySlot.GearSpecial:
                case InventorySlot.GearClass:
                    return CWC.Weapon.InventorySlot == ReceiverSlot;
                case InventorySlot.None:
                    return true;
                default:
                    return false;
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

        private static bool TryCreateHolder(BackpackItem bpItem, [MaybeNullWhen(false)] out IAmmoComp holder)
        {
            var item = bpItem.Instance;
            if (item.TryCastOut<BulletWeapon>(out var gun))
            {
                holder = new EmptyHolder(gun);
                return true;
            }
            else if (item.TryCastOut<SentryGunFirstPerson>(out var sentry))
            {
                if (CustomWeaponManager.TryGetSentry(sentry.Owner, out var sentryInfo))
                    holder = sentryInfo.cgc?.Gun ?? new SentryGunComp(sentryInfo.sentry);
                else
                    holder = new EmptyHolder(null);
                return true;
            }
            holder = null;
            return false;
        }

        class EmptyHolder : IAmmoComp
        {
            private readonly bool _isSentry;
            private readonly ItemEquippable? _item;

            public EmptyHolder(ItemEquippable? item)
            {
                _isSentry = item == null;
                _item = item;
            }

            public AmmoType AmmoType => _isSentry ? AmmoType.Class : _item!.AmmoType;

            public int GetCurrentClip()
            {
                if (_isSentry) return 0;
                return _item!.GetCurrentClip();
            }

            public int GetMaxClip()
            {
                if (_isSentry) return 0;
                return _item!.GetMaxClip();
            }

            public void SetCurrentClip(int clip)
            {
                if (_isSentry) return;
                _item!.SetCurrentClip(clip);
            }

            public CellSoundPlayer Sound => throw new NotImplementedException();
            public ItemEquippable Component => throw new NotImplementedException();
            public WeaponType Type => throw new NotImplementedException();
            public bool AllowBackstab => throw new NotImplementedException();
            public ArchetypeDataBlock ArchetypeData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool IsShotgun => throw new NotImplementedException();
            public Weapon.WeaponHitData VanillaHitData => throw new NotImplementedException();
            public FX_Pool TracerPool => throw new NotImplementedException();
            public float MaxRayDist { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }
    }
}
