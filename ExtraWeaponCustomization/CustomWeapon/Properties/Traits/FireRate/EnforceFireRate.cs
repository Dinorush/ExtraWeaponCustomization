using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using Gear;
using Player;
using System;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class EnforceFireRate :
        Trait,
        IGunProperty,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponPostFireContext>
    {
        private float _lastShotTime = 0f;
        private float _shotBuffer = 0f;
        private float _fixedTime = 0f;
        private readonly static float FixedDelta;
        
        static EnforceFireRate()
        {
            FixedDelta = Time.fixedDeltaTime;
        }

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

            if (extraShots == 0) return;

            _fixedTime = Time.time - Time.fixedTime;
            FPSCamera camera = CWC.Weapon.Owner.FPSCamera;
            FPS_RecoilSystem system = camera.m_recoilSystem;

            float delta = Clock.Delta;
            shotDelay = Math.Min(shotDelay, delta);
            // Modify delta time so FPS_Update() moves the correct amount
            Clock.Delta = shotDelay;
            for (int i = 0; i < extraShots; i++)
            {
                // Update camera to where it should be
                system.FPS_Update();
                camera.RotationUpdate();
                // Camera Ray only updates on fixed time
                if (!FSFAPIWrapper.hasFSF && (_fixedTime += shotDelay) > FixedDelta)
                {
                    camera.UpdateCameraRay();
                    _fixedTime -= FixedDelta;
                }
                archetype.OnFireShot();
            }

            Clock.Delta = delta;
            _shotBuffer -= extraShots;
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
