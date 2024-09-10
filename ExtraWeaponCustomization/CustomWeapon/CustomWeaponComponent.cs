using AK;
using ExtraWeaponCustomization.CustomWeapon.Properties;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using Gear;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExtraWeaponCustomization.CustomWeapon
{
    public sealed class CustomWeaponComponent : MonoBehaviour
    {
        public BulletWeapon Weapon { get; private set; }

        private readonly ContextController _contextController;

        private AutoAim? _autoAim;
        private bool _ownerSet;

        public bool CancelShot { get; set; }

        private readonly Dictionary<Type, Trait> _traits;
        // When canceling a shot, holds the next shot timer so we can reset back to it
        private float _lastShotTimer = 0f;
        private float _lastBurstTimer = 0f;
        private float _lastFireRate = 0f;
        public float CurrentFireRate { get; private set; }
        public float CurrentBurstDelay { get; private set; }

        private readonly float _fireRate;
        private readonly float _burstDelay;

        public CustomWeaponComponent(IntPtr value) : base(value) {
            _contextController = new ContextController();
            _traits = new Dictionary<Type, Trait>();

            BulletWeapon? bulletWeapon = GetComponent<BulletWeapon>();
            if (bulletWeapon == null)
                throw new ArgumentException("Parent Object", "Custom Weapon Component was added to an object without a BulletWeapon component.");
            Weapon = bulletWeapon;

            _fireRate = 1f / Math.Max(Weapon.m_archeType.ShotDelay(), CustomWeaponData.MinShotDelay);
            _lastFireRate = _fireRate;
            CurrentFireRate = _fireRate;
            _burstDelay = Weapon.m_archeType.BurstDelay();
            CurrentBurstDelay = _burstDelay;
            _ownerSet = false;
        }

        // Only runs on local player!
        public void OwnerInit()
        {
            if (_ownerSet) return;

            _autoAim?.OnEnable();
            _ownerSet = true;
        }

        public void SetToSync()
        {
            // Bots need full behavior but bots are pain and use different functions so idc for now
            Clear();
            _contextController.ChangeToSyncContexts();
            Register(CustomWeaponManager.Current.GetCustomWeaponData(Weapon.ArchetypeID));
            _autoAim = null;
        }

        public void Update()
        {
            if (_ownerSet)
                _autoAim?.Update();
        }

        public void OnEnable()
        {
            if (_ownerSet)
                _autoAim?.OnEnable();
        }

        public void OnDisable()
        {
            if (_ownerSet)
                _autoAim?.OnDisable();
        }

        [HideFromIl2Cpp]
        public void Invoke<TContext>(TContext context) where TContext : IWeaponContext => _contextController.Invoke(context);

        [HideFromIl2Cpp]
        public void Register(IWeaponProperty property)
        {
            if (property is AutoAim autoAim)
                _autoAim ??= autoAim;

            _contextController.Register(property);
            property.CWC = this;

            if (property is Trait trait)
                _traits.TryAdd(property.GetType(), trait);
        }

        [HideFromIl2Cpp]
        public void Register(CustomWeaponData? data)
        {
            if (data == null) return;

            List<IWeaponProperty> properties = data.Properties.ConvertAll(property => property.Clone());
            foreach (IWeaponProperty property in properties)
                Register(property);

            Invoke(new WeaponPostSetupContext());
        }

        public void Clear()
        {
            Invoke(new WeaponClearContext());
            _traits.Clear();
            _contextController.Clear();
            _autoAim = null;
            _ownerSet = false;
            CurrentFireRate = _fireRate;
            CurrentBurstDelay = _burstDelay;
            Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        [HideFromIl2Cpp]
        public bool HasTrait(Type type) => _traits.ContainsKey(type);
        [HideFromIl2Cpp]
        public Trait GetTrait(Type type) => _traits[type];

        public void StoreCancelShot()
        {
            if (!CancelShot)
            {
                Invoke(new WeaponCancelFireContext());
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
                if (archetype.m_archetypeData.FireMode == eWeaponFireMode.Burst)
                    archetype.TryCast<BWA_Burst>()!.m_burstCurrentCount = 0;
                return true;
            }
            return false;
        }

        public void UpdateStoredFireRate(BulletWeaponArchetype archetype)
        {
            _lastFireRate = CurrentFireRate;
            _lastShotTimer = archetype.m_nextShotTimer;
            _lastBurstTimer = archetype.m_nextBurstTimer;

            // Invoke callbacks that override base fire rate
            WeaponFireRateSetContext context = new(_fireRate);
            Invoke(context);

            // Invoke callbacks that modify current fire rate
            WeaponFireRateContext postContext = new(context.FireRate);
            Invoke(postContext);

            if (CurrentFireRate != postContext.Value)
            {
                CurrentFireRate = Math.Clamp(postContext.Value, 0.001f, CustomWeaponData.MaxFireRate);
                CurrentBurstDelay = _burstDelay * _fireRate / CurrentFireRate;
                RefreshSoundDelay();
            }
        }

        public void RefreshSoundDelay()
        {
            Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        public void ModifyFireRate(BulletWeaponArchetype archetype) {
            archetype.m_nextShotTimer = Clock.Time + 1f / CurrentFireRate;
            if (archetype.BurstIsDone())
                archetype.m_nextBurstTimer = Math.Max(Clock.Time + CurrentBurstDelay, archetype.m_nextShotTimer);
        }

        public void ModifyFireRate(BulletWeaponSynced synced)
        {
            synced.m_lastFireTime = Clock.Time + 1f / CurrentFireRate - Weapon.ArchetypeData.ShotDelay;
        }
    }
}
