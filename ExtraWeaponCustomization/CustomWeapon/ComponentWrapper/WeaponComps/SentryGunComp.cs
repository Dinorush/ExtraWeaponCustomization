using EWC.CustomWeapon.Enums;
using EWC.Dependencies;
using FX_EffectSystem;
using GameData;
using Gear;
using Player;
using System;

namespace EWC.CustomWeapon.ComponentWrapper.WeaponComps
{
    public class SentryGunComp : GunComp<SentryGunInstance>
    {
        private WeaponAudioDataBlock _audioData;
        private ArchetypeDataBlock _archetypeData;
        private readonly CellSoundPlayer _sound;
        private bool _allowBackstab;

        public bool IsFirstShot { get; set; } = true;

        public SentryGunComp(SentryGunInstance value) : base(value, value.ArchetypeData.ShotgunBulletCount > 0, value.ArchetypeData.FireMode)
        {
            _archetypeData = Value.ArchetypeData;
            _audioData = Value.AudioData;
            Firing = Value.m_firing.Cast<SentryGunInstance_Firing_Bullets>();
            Detection = Value.m_detection.Cast<SentryGunInstance_Detection>();
            _sound = Value.Sound;
            CostOfBullet = _archetypeData.CostOfBullet * Math.Max(1, _archetypeData.ShotgunBulletCount) * Value.ItemDataBlock.ClassAmmoCostFactor;
            _allowBackstab = ETCWrapper.CanDoBackDamage(_archetypeData.persistentID);
        }

        public readonly SentryGunInstance_Firing_Bullets Firing;
        public readonly SentryGunInstance_Detection Detection;

        public float CostOfBullet { get; private set; }

        public bool IsTargetTagged => Value.m_detection.TargetIsTagged;

        public override ArchetypeDataBlock ArchetypeData
        {
            get => _archetypeData;
            set
            {
                if (value == null) return;

                _archetypeData = value;
                Value.ArchetypeData = value;
                Firing.m_archetypeData = value;
                Detection.m_archetypeData = value;
                FireMode = value.FireMode;
                CostOfBullet = _archetypeData.CostOfBullet * Math.Max(1, _archetypeData.ShotgunBulletCount) * Value.ItemDataBlock.ClassAmmoCostFactor;
                _allowBackstab = ETCWrapper.CanDoBackDamage(value.persistentID);
            }
        }

        public override WeaponAudioDataBlock AudioData
        {
            get => _audioData;
            set
            {
                if (value == null) return;

                _audioData = value;
                Firing.m_audioData = value;
                Firing.m_audioFireSemi = BulletWeapon.GetRandomAudioEvents(value.eventOnSemiFire3D);
                Firing.m_audioFireBurst = BulletWeapon.GetRandomAudioEvents(value.eventOnBurstFire3D);
                Firing.m_audioFireAutoStart = BulletWeapon.GetRandomAudioEvents(value.eventOnAutoFireStart3D);
                Firing.m_audioFireAutoEnd = BulletWeapon.GetRandomAudioEvents(value.eventOnAutoFireEnd3D);
                Firing.m_audioChargeup = BulletWeapon.GetRandomAudioEvents(value.eventOnChargeup3D);
                Firing.m_audioCooldown = BulletWeapon.GetRandomAudioEvents(value.eventOnCooldown3D);
            }
        }

        public override Weapon.WeaponHitData VanillaHitData => SentryGunInstance_Firing_Bullets.s_weaponRayData;
        public override FX_Pool TracerPool => SentryGunInstance_Firing_Bullets.s_tracerPool;

        public override float MaxRayDist
        {
            get => Firing.MaxRayDist;
            set => Firing.MaxRayDist = value;
        }

        public override bool IsAiming => false;
        public override AmmoType AmmoType => AmmoType.Class;
        public override WeaponType Type => WeaponType.Sentry | WeaponType.Gun;
        public override CellSoundPlayer Sound => _sound;

        public override int GetCurrentClip() => (int) (Value.Ammo / CostOfBullet);
        public override int GetMaxClip() => (int)(Math.Max(Value.Ammo, Value.AmmoMaxCap) / CostOfBullet);
        public int GetMaxClip(out bool overflow)
        {
            overflow = Value.Ammo > Value.AmmoMaxCap;
            return GetMaxClip();
        }

        public override bool AllowBackstab => _allowBackstab || AllowBackstabOverride;
        public bool AllowBackstabOverride { get; set; } = false;

        public override void SetCurrentClip(int clip)
        {
            int bullets = (int) (Value.Ammo / CostOfBullet);
            if (clip == bullets) return;

            float leftovers = Value.Ammo - bullets * CostOfBullet;
            Value.Ammo = clip * CostOfBullet + leftovers;
            Value.m_sync.ForceReliableAmmoUpdate(Value.Ammo);
            Firing.UpdateAmmo();
        }

        public override float ModifyFireRate(float lastFireTime, float shotDelay, float burstDelay, float cooldownDelay)
        {
            Firing.m_fireBulletTimer = lastFireTime + shotDelay;
            if (FireMode == eWeaponFireMode.Burst && Firing.m_burstTimer > Clock.Time)
                Firing.m_burstTimer = Clock.Time + burstDelay;
            return Math.Max(Firing.m_fireBulletTimer, Firing.m_burstTimer);
        }
    }
}
