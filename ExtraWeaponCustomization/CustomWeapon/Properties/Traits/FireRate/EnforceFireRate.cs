using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
using Player;
using System;
using System.Text.Json;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Traits
{
    public sealed class EnforceFireRate :
        Trait,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponDamageContext>,
        IWeaponProperty<WeaponRecoilContext>
    {
        private CustomWeaponComponent? _cachedCWC = null;

        private float _lastShotTime = 0f;
        private float _shotBuffer = 0f;

        public void Invoke(WeaponPostStartFireContext context)
        {
            _shotBuffer = 0;
            _lastShotTime = Clock.Time;
        }

        public void Invoke(WeaponPostFireContext context)
        {
            BulletWeaponArchetype archetype = context.Weapon.m_archeType;
            if (_cachedCWC == null)
                _cachedCWC = context.Weapon.GetComponent<CustomWeaponComponent>();
            float shotDelay = 1f / _cachedCWC.CurrentFireRate;

            // Hit callback runs and applies damage bonus before this one, so we can safely reduce the buffer
            int extraShots = GetShotsInBuffer(context.Weapon);
            if (_cachedCWC.HasProperty(typeof(ReserveClip)))
                PlayerBackpackManager.GetBackpack(context.Weapon.Owner.Owner).AmmoStorage.UpdateBulletsInPack(context.Weapon.AmmoType, -extraShots);
            else
                archetype.m_weapon.m_clip -= extraShots;
            _shotBuffer -= extraShots;

            if (_lastShotTime != Clock.Time)
                _shotBuffer += (Clock.Time - _lastShotTime) / Math.Max(CustomWeaponData.MinShotDelay, shotDelay) - 1f;

            _lastShotTime = Clock.Time;
            // Need to update ammo since we modified the clip
            context.Weapon.UpdateAmmoStatus();
        }

        public void Invoke(WeaponDamageContext context)
        {
            // Won't apply on the first shot (no time delta available to use)
            context.Damage.AddMod(1f + GetShotsInBuffer(context.Weapon), Effects.StackType.Multiply);
        }

        public void Invoke(WeaponRecoilContext context)
        {
            // Won't apply on the first shot (no time delta available to use)
            context.AddMod(1f + GetShotsInBuffer(context.Weapon), Effects.StackType.Multiply);
        }

        private int GetShotsInBuffer(BulletWeapon weapon)
        {
            int cap = weapon.GetCurrentClip();
            if (_cachedCWC?.HasProperty(typeof(ReserveClip)) == true)
                cap = PlayerBackpackManager.GetBackpack(weapon.Owner.Owner).AmmoStorage.GetBulletsInPack(weapon.AmmoType);
          
            return Math.Min(cap, (int)_shotBuffer);
        }

        public override IWeaponProperty Clone()
        {
            return new EnforceFireRate();
        }

        public override void Serialize(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader, JsonSerializerOptions options) {}
    }
}
