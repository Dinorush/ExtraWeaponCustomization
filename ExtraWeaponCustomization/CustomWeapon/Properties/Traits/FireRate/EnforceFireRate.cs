using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using Player;
using System;
using System.Text.Json;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class EnforceFireRate :
        Trait,
        IGunProperty,
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
            BulletWeaponArchetype archetype = CWC.Gun!.m_archeType;
            if (_cachedCWC == null)
                _cachedCWC = CWC.Weapon.GetComponent<CustomWeaponComponent>();
            float shotDelay = 1f / _cachedCWC.CurrentFireRate;

            // Hit callback runs and applies damage bonus before this one, so we can safely reduce the buffer
            int extraShots = GetShotsInBuffer(CWC.Gun!);
            if (_cachedCWC.HasTrait(typeof(ReserveClip)))
                PlayerBackpackManager.GetBackpack(CWC.Gun!.Owner.Owner).AmmoStorage.UpdateBulletsInPack(CWC.Gun!.AmmoType, -extraShots);
            else
                archetype.m_weapon.m_clip -= extraShots;
            _shotBuffer -= extraShots;

            if (_lastShotTime != Clock.Time)
                _shotBuffer += (Clock.Time - _lastShotTime) / Math.Max(CustomWeaponData.MinShotDelay, shotDelay) - 1f;

            _lastShotTime = Clock.Time;
            // Need to update ammo since we modified the clip
            CWC.Gun!.UpdateAmmoStatus();
        }

        public void Invoke(WeaponDamageContext context)
        {
            // Won't apply on the first shot (no time delta available to use)
            context.Damage.AddMod(1f + GetShotsInBuffer(CWC.Gun!), Effects.StackType.Multiply);
        }

        public void Invoke(WeaponRecoilContext context)
        {
            // Won't apply on the first shot (no time delta available to use)
            context.AddMod(1f + GetShotsInBuffer(CWC.Gun!), Effects.StackType.Multiply);
        }

        private int GetShotsInBuffer(BulletWeapon weapon)
        {
            int cap = weapon.GetCurrentClip();
            if (_cachedCWC?.HasTrait(typeof(ReserveClip)) == true)
                cap = PlayerBackpackManager.GetBackpack(weapon.Owner.Owner).AmmoStorage.GetBulletsInPack(weapon.AmmoType);
          
            return Math.Min(cap, (int)_shotBuffer);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }

        public override void DeserializeProperty(string property, ref Utf8JsonReader reader) {}
    }
}
