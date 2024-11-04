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
        IWeaponProperty<WeaponRecoilContext>
    {
        private float _lastShotTime = 0f;
        private float _shotBuffer = 0f;

        public void Invoke(WeaponPostStartFireContext context)
        {
            _shotBuffer = 0;
            _lastShotTime = Clock.Time;
        }

        public void Invoke(WeaponPostFireContext context)
        {
            // Acts as a lock against recursive calls and first shot
            if (_lastShotTime == Clock.Time) return;

            BulletWeaponArchetype archetype = CWC.Gun!.m_archeType;
            float shotDelay = 1f / CWC.CurrentFireRate;

            _shotBuffer += (Clock.Time - _lastShotTime) / shotDelay - 1f;
            int extraShots = GetShotsInBuffer(CWC.Gun!);

            _lastShotTime = Clock.Time;
            for (int i = 0; i < extraShots; i++)
                archetype.OnFireShot();
            _shotBuffer -= extraShots;
        }

        public void Invoke(WeaponRecoilContext context)
        {
            // Recoil is not accumulative, so need to modify.
            context.AddMod(1f + GetShotsInBuffer(CWC.Gun!), Effects.StackType.Multiply);
        }

        private int GetShotsInBuffer(BulletWeapon weapon)
        {
            int cap = weapon.GetCurrentClip();
            if (CWC.HasTrait(typeof(ReserveClip)))
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
