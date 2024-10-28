using AK;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Gear;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.CustomWeapon
{
    public sealed class CustomWeaponComponent : MonoBehaviour
    {
        public readonly ItemEquippable Weapon;
        public readonly BulletWeapon? Gun;
        public readonly bool IsGun;
        public readonly MeleeWeaponFirstPerson? Melee;
        public readonly bool IsMelee;

        private readonly PropertyController _propertyController;

        private AutoAim? _autoAim;
        [HideFromIl2Cpp]
        internal AutoAim? AutoAim
        {
            get { return _autoAim; }
            set { _autoAim = value; if (OwnerSet) _autoAim?.OnEnable(); }
        }

        private bool OwnerSet => _ownerPtr != IntPtr.Zero;
        private IntPtr _ownerPtr = IntPtr.Zero;

        public bool CancelShot { get; set; }

        // When canceling a shot, holds the next shot timer so we can reset back to it
        private float _lastShotTimer = 0f;
        private float _lastBurstTimer = 0f;
        private float _lastFireRate = 0f;
        private bool _synced = false;
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
            IsGun = Gun != null;
            IsMelee = !IsGun;

            _propertyController = new(IsGun);
            if (IsGun)
            {
                _fireRate = 1f / Math.Max(Gun!.m_archeType.ShotDelay(), CustomWeaponData.MinShotDelay);
                _lastFireRate = _fireRate;
                CurrentFireRate = _fireRate;
                _burstDelay = Gun.m_archeType.BurstDelay();
                CurrentBurstDelay = _burstDelay;
            }
            enabled = false;
        }

        // Only runs on local player!
        public void OwnerInit()
        {
            IntPtr ptr = Gun!.m_archeType.m_owner.Pointer;
            if (ptr == IntPtr.Zero || ptr == _ownerPtr || !enabled) return;

            _ownerPtr = Gun!.m_archeType.m_owner.Pointer;
            Invoke(StaticContext<WeaponOwnerSetContext>.Instance);
        }

        public void SetToSync()
        {
            if (_synced) return;
            // Bots need full behavior but bots are pain and use different functions so idc for now
            Clear();
            _propertyController.ChangeToSyncContexts();
            _synced = true;
            Register(CustomWeaponManager.Current.GetCustomGunData(Weapon.ArchetypeID));
            _autoAim = null;
        }

        private void Update()
        {
            if (OwnerSet)
                _autoAim?.Update();
        }

        private void OnEnable()
        {
            if (OwnerSet)
                _autoAim?.OnEnable();
        }

        private void OnDisable()
        {
            if (OwnerSet)
                _autoAim?.OnDisable();
        }

        [HideFromIl2Cpp]
        public TContext Invoke<TContext>(TContext context) where TContext : IWeaponContext => _propertyController.Invoke(context);

        [HideFromIl2Cpp]
        public void Register(CustomWeaponData? data = null)
        {
            if (enabled) return; // Don't want to register data twice
            enabled = true;

            if (data == null)
            {
                data = IsGun ? CustomWeaponManager.Current.GetCustomGunData(Weapon.ArchetypeID) : CustomWeaponManager.Current.GetCustomMeleeData(Weapon.MeleeArchetypeData.persistentID);
                if (data == null) return;
            }

            // If called by Activate(), i.e. without data, need to ensure it gets set to sync when applicable
            if (!_synced && Weapon.TryCast<BulletWeaponSynced>() != null)
            {
                SetToSync();
                return;
            }

            _propertyController.Init(this, data.Properties.Clone());
            if (Gun?.m_archeType?.m_owner != null)
                OwnerInit();
        }

        public void Clear()
        {
            Invoke(StaticContext<WeaponClearContext>.Instance);
            _propertyController.Clear();
            _autoAim = null;
            _ownerPtr = IntPtr.Zero;
            enabled = false;
            CurrentFireRate = _fireRate;
            CurrentBurstDelay = _burstDelay;
            Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        [HideFromIl2Cpp]
        internal void ActivateNode(PropertyNode node) => _propertyController.SetActive(node, true);
        [HideFromIl2Cpp]
        internal void DeactivateNode(PropertyNode node) => _propertyController.SetActive(node, false);

        [HideFromIl2Cpp]
        public bool HasTrait(Type type) => _propertyController.HasTrait(type);
        [HideFromIl2Cpp]
        public Trait GetTrait(Type type) => _propertyController.GetTrait(type);
        [HideFromIl2Cpp]
        public bool TryGetTrait(Type type, [MaybeNullWhen(false)] out Trait trait) => _propertyController.TryGetTrait(type, out trait);

        [HideFromIl2Cpp]
        internal ITriggerCallbackSync GetTriggerSync(ushort id) => _propertyController.GetTriggerSync(id);

        [HideFromIl2Cpp]
        public ContextController GetContextController() => _propertyController.GetContextController();
        public bool HasTempProperties() => _propertyController.HasTempProperties();

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
            if (IsGun)
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
