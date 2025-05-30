﻿using EWC.CustomWeapon.CustomShot;
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
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>
    {
        private int _lastSyncShotCount = 0;
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

            // If semi-auto or burst, calculate if they've continuously fired
            // based on whether this is the first frame since they could fire
            if (CWC.GunFireMode != eWeaponFireMode.Auto)
            {
                float intendedShotTime = CWC.GunArchetype!.m_nextBurstTimer;
                float maxContinueTime = intendedShotTime + Clock.Delta;
                if (Clock.Time <= maxContinueTime)
                {
                    _lastShotTime = intendedShotTime;
                    return;
                }
            }
            _lastShotTime = Clock.Time;
        }

        public void Invoke(WeaponPostFireContextSync context)
        {
            var weapon = CWC.Gun!.Cast<BulletWeaponSynced>();
            if (_lastSyncShotCount == 0)
            {
                _lastSyncShotCount = weapon.m_shotsToFire;
                _lastShotTime = Clock.Time;
                _shotBuffer = 0;
                return;
            }

            _shotBuffer += Math.Max(0, (Clock.Time - _lastShotTime) * CWC.CurrentFireRate - 1f);
            int extraShots = (int)_shotBuffer;
            weapon.m_shotsToFire = Math.Max(0, weapon.m_shotsToFire - extraShots);
            _lastSyncShotCount = weapon.m_shotsToFire;
            _lastShotTime = Clock.Time;
            _shotBuffer -= extraShots;
        }

        public void Invoke(WeaponPostFireContext context)
        {
            // Acts as a lock against recursive calls and first shot
            if (_lastShotTime == Clock.Time) return;

            _shotBuffer += Math.Max(0, (Clock.Time - _lastShotTime) * CWC.CurrentFireRate - 1f);
            int extraShots = GetShotsInBuffer(CWC.Gun!);
            _lastShotTime = Clock.Time;

            if (extraShots == 0) return;

            _fixedTime = Time.time - Time.fixedTime;
            FPSCamera camera = CWC.Weapon.Owner.FPSCamera;
            FPS_RecoilSystem system = camera.m_recoilSystem;

            float delta = Clock.Delta;
            float shotDelay = 1f / CWC.CurrentFireRate;
            shotDelay = Math.Min(shotDelay, delta);
            // Modify delta time so FPS_Update() moves the correct amount
            Clock.Delta = shotDelay;
            CWC.ShotComponent!.CancelAllFX = true;
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
                CWC.GunArchetype!.OnFireShot();
            }
            CWC.ShotComponent!.CancelAllFX = false;

            Clock.Delta = delta;
            _shotBuffer -= extraShots;
        }

        private int GetShotsInBuffer(BulletWeapon weapon)
        {
            int cap = weapon.GetCurrentClip();
            if (CWC.HasTrait<ReserveClip>())
                cap += PlayerBackpackManager.GetBackpack(weapon.Owner.Owner).AmmoStorage.GetBulletsInPack(weapon.AmmoType);

            if (CWC.TryGetBurstArchetype(out var arch))
                cap = Math.Min(cap, arch.m_burstCurrentCount);
          
            return Math.Min(cap, (int)_shotBuffer);
        }

        public override void Serialize(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", GetType().Name);
            writer.WriteEndObject();
        }
    }
}
