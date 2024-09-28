using AK;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EWC.CustomWeapon
{
    public sealed class CustomWeaponComponent : MonoBehaviour
    {
        public readonly ItemEquippable Weapon;
        public readonly BulletWeapon? Gun;
        public readonly MeleeWeaponFirstPerson? Melee;

        private readonly PropertyController _propertyController;

        private AutoAim? _autoAim;
        [HideFromIl2Cpp]
        internal AutoAim? AutoAim
        {
            get { return _autoAim; }
            set { _autoAim = value; if (_ownerSet) _autoAim?.OnEnable(); }
        }

        private bool _ownerSet;

        public bool CancelShot { get; set; }

        // When canceling a shot, holds the next shot timer so we can reset back to it
        private float _lastShotTimer = 0f;
        private float _lastBurstTimer = 0f;
        private float _lastFireRate = 0f;
        public float CurrentFireRate { get; private set; }
        public float CurrentBurstDelay { get; private set; }

        private float _fireRate;
        private float _burstDelay;

        public CustomWeaponComponent(IntPtr value) : base(value) {
            ItemEquippable? item = GetComponent<ItemEquippable>();
            if (item == null)
                throw new ArgumentException("Parent Object", "Custom Weapon Component was added to an object without an ItemEquippable component.");
            Weapon = item;
            Gun = item.TryCast<BulletWeapon>();
            Melee = item.TryCast<MeleeWeaponFirstPerson>();

            _propertyController = new(Gun != null);

            if (Gun != null)
            {
                _fireRate = 1f / Math.Max(Gun.m_archeType.ShotDelay(), CustomWeaponData.MinShotDelay);
                _lastFireRate = _fireRate;
                CurrentFireRate = _fireRate;
                _burstDelay = Gun.m_archeType.BurstDelay();
                CurrentBurstDelay = _burstDelay;
            }
            _ownerSet = false;
        }

        // Only runs on local player!
        public void OwnerInit()
        {
            if (_ownerSet) return;

            _ownerSet = true;
            Invoke(StaticContext<WeaponOwnerSetContext>.Instance);
        }

        public void SetToSync()
        {
            // Bots need full behavior but bots are pain and use different functions so idc for now
            Clear();
            _propertyController.ChangeToSyncContexts();
            Register(CustomWeaponManager.Current.GetCustomGunData(Weapon.ArchetypeID));
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
        public void Invoke<TContext>(TContext context) where TContext : IWeaponContext => _propertyController.Invoke(context);

        [HideFromIl2Cpp]
        public void Register(CustomWeaponData? data)
        {
            if (data == null) return;

            List<IWeaponProperty> properties = data.Properties.ConvertAll(property => property.Clone());
            _propertyController.Init(this, new PropertyList(properties));
        }

        public void Clear()
        {
            Invoke(StaticContext<WeaponClearContext>.Instance);
            _propertyController.Clear();
            _autoAim = null;
            _ownerSet = false;
            CurrentFireRate = _fireRate;
            CurrentBurstDelay = _burstDelay;
            Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        [HideFromIl2Cpp]
        internal void Activate(PropertyNode node) => _propertyController.SetActive(node, true);
        [HideFromIl2Cpp]
        internal void Deactivate(PropertyNode node) => _propertyController.SetActive(node, false);

        [HideFromIl2Cpp]
        public bool HasTrait(Type type) => _propertyController.HasTrait(type);
        [HideFromIl2Cpp]
        public Trait GetTrait(Type type) => _propertyController.GetTrait(type);

        [HideFromIl2Cpp]
        internal ITriggerCallbackSync GetTriggerSync(ushort id) => _propertyController.GetTriggerSync(id);

        public void StoreCancelShot()
        {
            if (!CancelShot)
            {
                Invoke(StaticContext<WeaponCancelFireContext>.Instance);
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

        public void RefreshArchetypeCache()
        {
            if (Gun != null)
            {
                _fireRate = 1f / Math.Max(Gun.m_archeType.ShotDelay(), CustomWeaponData.MinShotDelay);
                _burstDelay = Gun.m_archeType.BurstDelay();
                UpdateStoredFireRate(Gun.m_archeType);
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
