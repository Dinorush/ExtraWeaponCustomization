using AK;
using EWC.Attributes;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using GameData;
using Gear;
using Il2CppInterop.Runtime.Injection;
using System;

namespace EWC.CustomWeapon
{
    public sealed class CustomGunComponent : CustomWeaponComponent
    {
        public readonly IGunComp Gun;

        // To avoid patching an update function, we have to reset things back.
        // We need this to keep track of cancels across multiple functions.
        public bool CancelShot { get; set; }

        // When canceling a shot, holds the next shot timer so we can reset back to it.
        private float _lastShotTimer = 0f;
        private float _lastBurstTimer = 0f;
        private float _lastFireRate = 0f;

        // Holds when the previous shot was fired.
        // When canceling a shot that ends a burst/full-auto, we need to set cooldown based on the last shot.
        private float _lastFireTime = 0f;

        private float _currentChargeSpeed = 1f;
        private float _baseChargeTime;
        private float _startChargeTime = 0f;

        public float CurrentFireRate { get; private set; }
        public float CurrentBurstDelay { get; private set; }
        public float CurrentCooldownDelay { get; private set; }
        public float CurrentChargeMod { get; private set; }
        public float BaseFireRate { get; private set; }

        private float _burstDelay;
        private float _cooldownDelay;

        public CustomGunComponent(IntPtr value) : base(value) 
        {
            Gun = (IGunComp)Weapon;
            var archData = Gun.ArchetypeData;
            CurrentFireRate = _lastFireRate = BaseFireRate = 1f / Math.Max(archData.ShotDelay, CustomWeaponData.MinShotDelay);
            CurrentBurstDelay = _burstDelay = archData.BurstDelay;
            CurrentCooldownDelay = _cooldownDelay = archData.SpecialCooldownTime;
            CurrentChargeMod = 1f;
            _baseChargeTime = archData.SpecialChargetupTime;
        }

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomGunComponent>();
        }

        public override void OnWield()
        {
            RefreshSoundDelay();
            base.OnWield();
            Gun.ArchetypeData.SpecialChargetupTime *= CurrentChargeMod;
        }

        public override void OnUnWield()
        {
            base.OnUnWield();
            Gun.ArchetypeData.SpecialChargetupTime = _baseChargeTime;
        }

        protected override void Update()
        {
            base.Update();

            if (Owner.IsType(Enums.OwnerType.Local))
            {
                var arch = ((LocalGunComp)Gun).GunArchetype;
                if (!arch.m_inChargeup)
                    _startChargeTime = 0f;
                else if (_startChargeTime == 0f)
                {
                    _startChargeTime = Clock.Time;
                    Invoke(StaticContext<WeaponChargeStartContext>.Instance);
                }
            }
        }

        public override void Clear()
        {
            base.Clear();
            CurrentFireRate = BaseFireRate;
            CurrentBurstDelay = _burstDelay;
            CurrentCooldownDelay = _cooldownDelay;
            Gun.ArchetypeData.SpecialChargetupTime = _baseChargeTime;
            CurrentChargeMod = 1f;
            _currentChargeSpeed = 1f;
            if (!_destroyed)
                Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        public void StoreCancelShot()
        {
            if (!CancelShot)
            {
                Invoke(StaticContext<WeaponFireCanceledContext>.Instance);
                CancelShot = true;
            }
        }

        public bool ResetShotIfCancel(BulletWeaponArchetype archetype)
        {
            if (CancelShot)
            {
                archetype.m_fireHeld = false;
                archetype.m_nextShotTimer = _lastShotTimer;
                archetype.m_nextBurstTimer = _lastBurstTimer;
                CurrentFireRate = _lastFireRate;
                Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
                if (((LocalGunComp)Gun).TryGetBurstArchetype(out var arch))
                    arch.m_burstCurrentCount = 0;
                return true;
            }
            return false;
        }

        public void RefreshArchetypeCache(ArchetypeDataBlock oldBlock)
        {
            var archData = Gun.ArchetypeData;
            BaseFireRate = 1f / Math.Max(archData.ShotDelay, CustomWeaponData.MinShotDelay);
            _burstDelay = archData.BurstDelay;
            _cooldownDelay = archData.SpecialCooldownTime;

            if (Owner.IsType(Enums.OwnerType.Local))
            {
                oldBlock.SpecialChargetupTime = _baseChargeTime;
                _baseChargeTime = archData.SpecialChargetupTime;
                if (SpreadController.Active) // HACK - detect when equipped
                    archData.SpecialChargetupTime = _baseChargeTime * CurrentChargeMod;
            }
            UpdateStoredFireRate();
        }

        public void RefreshSoundDelay()
        {
            Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        public void NotifyShotFired() => _lastFireTime = Clock.Time;

        public void UpdateChargeTime(bool forceUpdateTime = false)
        {
            float newChargeSpeed = Math.Max(0.001f, Invoke(new WeaponChargeSpeedContext()).Value);
            if (newChargeSpeed != _currentChargeSpeed)
            {
                _currentChargeSpeed = Math.Max(newChargeSpeed, 0.001f);
                CurrentChargeMod = 1f / _currentChargeSpeed;
                Gun.ArchetypeData.SpecialChargetupTime = _baseChargeTime * CurrentChargeMod;
                // JFS - Should only run locally.
                if (forceUpdateTime)
                {
                    var arch = ((LocalGunComp)Gun).GunArchetype;
                    if (arch.m_inChargeup)
                        arch.m_chargeupTimer = _startChargeTime + _baseChargeTime * CurrentChargeMod;
                }
            }
        }

        public void UpdateStoredFireRate(bool isTagged = false)
        {
            _lastFireRate = CurrentFireRate;
            if (Owner.IsType(Enums.OwnerType.Local))
            {
                var arch = ((LocalGunComp)Gun).GunArchetype;
                _lastShotTimer = arch.m_nextShotTimer;
                _lastBurstTimer = arch.m_nextBurstTimer;
            }

            float fireRate = BaseFireRate;
            if (Owner.IsType(Enums.OwnerType.Sentry))
            {
                float invMod = isTagged ? Gun.ArchetypeData.Sentry_ShotDelayTagMulti : 1f;
                invMod = AgentModifierManager.ApplyModifier(Owner.Player, AgentModifier.SentryGunSpeed, invMod);
                fireRate /= invMod;
            }

            float newFireRate = Invoke(new WeaponFireRateContext(fireRate)).Value;
            newFireRate = Math.Clamp(newFireRate, 0.001f, CustomWeaponData.MaxFireRate);
            if (CurrentFireRate != newFireRate)
            {
                CurrentFireRate = newFireRate;
                CurrentBurstDelay = _burstDelay * fireRate / CurrentFireRate;
                CurrentCooldownDelay = _cooldownDelay * fireRate / CurrentFireRate;
                RefreshSoundDelay();
            }
        }

        public void ModifyFireRate()
        {
            float nextShotTime = Gun.ModifyFireRate(_lastFireTime, 1f / CurrentFireRate, CurrentBurstDelay, CurrentCooldownDelay);
            Invoke(new WeaponShotCooldownContext(nextShotTime));
        }
    }
}
