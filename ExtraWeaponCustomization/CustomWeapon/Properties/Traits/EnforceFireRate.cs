using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Dependencies;
using Player;
using System;
using System.Text.Json;
using UnityEngine;

namespace EWC.CustomWeapon.Properties.Traits
{
    public sealed class EnforceFireRate :
        Trait,
        IWeaponProperty<WeaponShotCooldownContext>,
        IWeaponProperty<WeaponPostStartFireContext>,
        IWeaponProperty<WeaponPostFireContext>,
        IWeaponProperty<WeaponPostFireContextSync>
    {
        private int _lastSyncShotCount = 0;
        private float _nextShotTime = 0f;
        private float _shotBuffer = 0f;
        private float _fixedTime = 0f;
        private readonly static float FixedDelta;
        
        static EnforceFireRate()
        {
            FixedDelta = Time.fixedDeltaTime;
        }

        protected override OwnerType ValidOwnerType => OwnerType.Local | OwnerType.Unmanaged;
        protected override WeaponType RequiredWeaponType => WeaponType.Gun;

        public void Invoke(WeaponShotCooldownContext context)
        {
            _nextShotTime = context.NextShotTime;
        }

        public void Invoke(WeaponPostStartFireContext context)
        {
            // If semi-auto or burst, calculate if they've continuously fired
            // based on whether this is the first frame since they could fire
            var gun = (LocalGunComp) CWC.Weapon;
            if (gun.FireMode != eWeaponFireMode.Auto)
            {
                float maxContinueTime = _nextShotTime + Clock.Delta;
                if (Clock.Time <= maxContinueTime)
                    return;
            }
            _shotBuffer = 0;
            _nextShotTime = 0;
        }

        public void Invoke(WeaponPostFireContextSync context)
        {
            var weapon = ((SyncedGunComp)CWC.Weapon).Value;
            if (_lastSyncShotCount == 0)
            {
                _lastSyncShotCount = weapon.m_shotsToFire;
                _shotBuffer = 0;
                return;
            }

            _shotBuffer += Math.Max(0, (Clock.Time - _nextShotTime) * CGC.CurrentFireRate);
            int extraShots = (int)_shotBuffer;
            weapon.m_shotsToFire = Math.Max(0, weapon.m_shotsToFire - extraShots);
            _lastSyncShotCount = weapon.m_shotsToFire;
            _shotBuffer -= extraShots;
        }

        public void Invoke(WeaponPostFireContext context)
        {
            // Acts as a lock against recursive calls and first shot
            if (_nextShotTime == 0) return;

            _shotBuffer += Math.Max(0, (Clock.Time - _nextShotTime) * CGC.CurrentFireRate);
            int extraShots = GetShotsInBuffer();
            _nextShotTime = 0;

            if (extraShots == 0) return;

            _fixedTime = Time.time - Time.fixedTime;
            FPSCamera camera = CWC.Owner.Player!.FPSCamera;
            FPS_RecoilSystem system = camera.m_recoilSystem;
            var gun = (LocalGunComp)CWC.Weapon;

            float delta = Clock.Delta;
            float shotDelay = 1f / CGC.CurrentFireRate;
            shotDelay = Math.Min(shotDelay, delta);
            // Modify delta time so FPS_Update() moves the correct amount
            Clock.Delta = shotDelay;
            CGC.ShotComponent.CancelAllFX = true;
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
                gun.GunArchetype.OnFireShot();
            }
            CGC.ShotComponent.CancelAllFX = false;

            Clock.Delta = delta;
            _shotBuffer -= extraShots;
        }

        private int GetShotsInBuffer()
        {
            var gun = (LocalGunComp)CWC.Weapon;
            int cap = gun.GetCurrentClip();
            if (CWC.HasTrait<ReserveClip>())
                cap += PlayerBackpackManager.GetBackpack(CWC.Owner.Player!.Owner).AmmoStorage.GetBulletsInPack(CWC.Weapon.AmmoType);

            if (gun.TryGetBurstArchetype(out var arch))
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
