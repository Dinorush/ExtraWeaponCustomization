using System;
using Player;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;
using System.Collections.Generic;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects.Triggers;
using System.Linq;
using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class AmmoMod :
        Effect,
        IWeaponProperty<WeaponPreFireContext>
    {
        public float ClipChange { get; set; } = 0;
        public float ReserveChange { get; set; } = 0;
        public bool OverflowToReserve { get; set; } = true;
        public bool PullFromReserve { get; set; } = false;

        private float _clipBuffer = 0;
        private float _reserveBuffer = 0;
        private float _lastFireTime = 0;

        public void Invoke(WeaponPreFireContext context)
        {
            _lastFireTime = Clock.Time;
        }

        public override void TriggerReset()
        {
            _clipBuffer = 0;
            _reserveBuffer = 0;
        }

        public override void TriggerApply(List<TriggerContext> triggerList)
        {
            BulletWeapon weapon = triggerList[0].context.Weapon;
            float triggers = triggerList.Sum(context => context.triggerAmt);
            _clipBuffer += ClipChange * triggers;
            _reserveBuffer += ReserveChange * triggers;

            if (_clipBuffer < 1 && _clipBuffer > -1 && _reserveBuffer < 1 && _reserveBuffer > -1) return;
            
            // Ammo decrements after this callback if on kill/shot/hit, need to account for that.
            // But if this weapon didn't get the kill (e.g. DOT kill), shouldn't do that.
            int accountForShot = Clock.Time == _lastFireTime ? 1 : 0;

            // Calculate the actual changes we can make to clip/ammo
            PlayerAmmoStorage ammoStorage = PlayerBackpackManager.GetBackpack(weapon.Owner.Owner).AmmoStorage;
            int clipChange = (int) (PullFromReserve ? Math.Min(_clipBuffer, ammoStorage.GetBulletsInPack(weapon.AmmoType)) : _clipBuffer);
            int newClip = Math.Clamp(weapon.GetCurrentClip() + clipChange, accountForShot, weapon.GetMaxClip() + accountForShot);

            // If we overflow/underflow the magazine, send the rest to reserves (if not pulling from reserves)
            int bonusReserve = OverflowToReserve ? clipChange - (newClip - weapon.GetCurrentClip()) : 0;
            clipChange = newClip - weapon.GetCurrentClip();

            int reserveChange = (int) (PullFromReserve ? _reserveBuffer - clipChange : _reserveBuffer + bonusReserve);

            _clipBuffer -= (int) _clipBuffer;
            _reserveBuffer -= (int) _reserveBuffer;

            weapon.SetCurrentClip(newClip);

            // Need to update UI since UpdateBulletsInPack does it without including the clip
            ammoStorage.UpdateBulletsInPack(weapon.AmmoType, reserveChange);
            ammoStorage.UpdateSlotAmmoUI(ammoStorage.m_ammoStorage[(int) weapon.AmmoType], newClip);
        }

        public override IWeaponProperty Clone()
        {
            AmmoMod copy = new()
            {
                ClipChange = ClipChange,
                ReserveChange = ReserveChange,
                OverflowToReserve = OverflowToReserve,
                PullFromReserve = PullFromReserve,
                Trigger = Trigger?.Clone()
            };
            return copy;
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ClipChange), ClipChange);
            writer.WriteNumber(nameof(ReserveChange), ReserveChange);
            writer.WriteBoolean(nameof(OverflowToReserve), OverflowToReserve);
            writer.WriteBoolean(nameof(PullFromReserve), PullFromReserve);
            SerializeTrigger(writer, options);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            base.DeserializeProperty(property, ref reader, options);
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
                default:
                    break;
            }
        }
    }
}
