using System;
using Player;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public sealed class AmmoMod :
        Effect,
        IWeaponProperty<WeaponPreFireContext>,
        IWeaponProperty<WeaponTriggerContext>
    {
        public float ClipChange { get; set; } = 0;
        public float ReserveChange { get; set; } = 0;
        public float Cooldown { get; set; } = 0f;
        public bool OverflowToReserve { get; set; } = true;
        public bool PullFromReserve { get; set; } = false;
        public TriggerType TriggerType { get; set; } = TriggerType.Invalid;

        private float _clipBuffer = 0;
        private float _reserveBuffer = 0;
        private float _lastFireTime = 0;
        private float _lastTriggerTime = 0;

        public void Invoke(WeaponPreFireContext context)
        {
            _lastFireTime = Clock.Time;
        }

        public void Invoke(WeaponTriggerContext context)
        {
            if (!context.Type.IsType(TriggerType) || Clock.Time < _lastTriggerTime + Cooldown) return;

            float damageMod = 1f;
            if (context.Type.IsType(TriggerType.OnDamage))
                damageMod = ((WeaponOnDamageContext) context).Damage;

            _clipBuffer += ClipChange * damageMod;
            _reserveBuffer += ReserveChange * damageMod;

            if (_clipBuffer < 1 && _clipBuffer > -1 && _reserveBuffer < 1 && _reserveBuffer > -1) return;
            
            // Ammo decrements after this callback if on kill/shot/hit, need to account for that.
            // But if this weapon didn't get the kill (e.g. DOT kill), shouldn't do that.
            int accountForShot = Clock.Time == _lastFireTime ? 1 : 0;

            // Calculate the actual changes we can make to clip/ammo
            PlayerAmmoStorage ammoStorage = PlayerBackpackManager.GetBackpack(context.Weapon.Owner.Owner).AmmoStorage;
            int clipChange = (int) (PullFromReserve ? Math.Min(_clipBuffer, ammoStorage.GetBulletsInPack(context.Weapon.AmmoType)) : _clipBuffer);
            int newClip = Math.Clamp(context.Weapon.GetCurrentClip() + clipChange, accountForShot, context.Weapon.GetMaxClip() + accountForShot);

            // If we overflow/underflow the magazine, send the rest to reserves (if not pulling from reserves)
            int bonusReserve = OverflowToReserve ? clipChange - (newClip - context.Weapon.GetCurrentClip()) : 0;
            clipChange = newClip - context.Weapon.GetCurrentClip();

            int reserveChange = (int) (PullFromReserve ? _reserveBuffer - clipChange : _reserveBuffer + bonusReserve);

            _clipBuffer -= (int) _clipBuffer;
            _reserveBuffer -= (int) _reserveBuffer;

            context.Weapon.SetCurrentClip(newClip);

            // Need to update UI since UpdateBulletsInPack does it without including the clip
            ammoStorage.UpdateBulletsInPack(context.Weapon.AmmoType, reserveChange);
            ammoStorage.UpdateSlotAmmoUI(ammoStorage.m_ammoStorage[(int) context.Weapon.AmmoType], newClip);
            _lastTriggerTime = Clock.Time;
        }

        public IWeaponProperty Clone()
        {
            AmmoMod copy = new()
            {
                ClipChange = ClipChange,
                ReserveChange = ReserveChange,
                Cooldown = Cooldown,
                OverflowToReserve = OverflowToReserve,
                PullFromReserve = PullFromReserve,
                TriggerType = TriggerType
            };
            return copy;
        }

        public void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteNumber(nameof(ClipChange), ClipChange);
            writer.WriteNumber(nameof(ReserveChange), ReserveChange);
            writer.WriteNumber(nameof(Cooldown), Cooldown);
            writer.WriteBoolean(nameof(OverflowToReserve), OverflowToReserve);
            writer.WriteBoolean(nameof(PullFromReserve), PullFromReserve);
            writer.WriteString(nameof(TriggerType), TriggerType.ToString());
            writer.WriteEndObject();
        }

        public void DeserializeProperty(string property, ref Utf8JsonReader reader)
        {
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
                case "cooldown":
                    Cooldown = reader.GetSingle();
                    break;
                case "overflowtoreserve":
                case "overflow":
                    OverflowToReserve = reader.GetBoolean();
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
