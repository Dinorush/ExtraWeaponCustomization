using AK;
using EWC.Attributes;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Properties.Effects.Spread;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using Il2CppInterop.Runtime.Injection;
using System;

namespace EWC.CustomWeapon
{
    public sealed class CustomGunComponent : CustomWeaponComponent
    {
        public readonly SpreadController SpreadController;
        public readonly CustomShotComponent ShotComponent;
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

        public float CurrentFireRate { get; private set; }
        public float CurrentBurstDelay { get; private set; }
        public float CurrentCooldownDelay { get; private set; }
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
            ShotComponent = new(this);
            SpreadController = new(Owner.IsType(Enums.OwnerType.Local));
        }

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomGunComponent>();
        }

        public override void OnWield()
        {
            SpreadController.Active = true;
            RefreshSoundDelay();
            base.OnWield();
        }

        public override void OnUnWield()
        {
            SpreadController.Active = false;
            base.OnUnWield();
        }

        public override void Clear()
        {
            base.Clear();
            SpreadController.Reset();
            CurrentFireRate = BaseFireRate;
            CurrentBurstDelay = _burstDelay;
            CurrentCooldownDelay = _cooldownDelay;
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

        public void RefreshArchetypeCache()
        {
            var archData = Gun.ArchetypeData;
            BaseFireRate = 1f / Math.Max(archData.ShotDelay, CustomWeaponData.MinShotDelay);
            _burstDelay = archData.BurstDelay;
            _cooldownDelay = archData.SpecialCooldownTime;
            UpdateStoredFireRate();
        }

        public void RefreshSoundDelay()
        {
            Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        public void NotifyShotFired() => _lastFireTime = Clock.Time;

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
            if (CurrentFireRate != newFireRate)
            {
                CurrentFireRate = Math.Clamp(newFireRate, 0.001f, CustomWeaponData.MaxFireRate);
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
