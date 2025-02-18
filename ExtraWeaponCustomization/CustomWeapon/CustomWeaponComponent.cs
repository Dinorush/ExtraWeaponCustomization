using AK;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
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

        public bool IsLocal { get; private set; } = true;
        private bool OwnerSet => _ownerPtr != IntPtr.Zero;
        private IntPtr _ownerPtr = IntPtr.Zero;

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

        // Used to prevent hit callbacks from firing. For some effects to prevent infinite recursion.
        private int _ignoreStack = 0;
        public bool RunHitTriggers
        {
            get { return _ignoreStack == 0; }
            set 
            {
                if (!value)
                {
                    if (_ignoreStack++ == 0)
                        _propertyController.GetContextController().BlacklistContext<WeaponHitContextBase>();
                }
                else if (value && _ignoreStack > 0)
                {
                    if (--_ignoreStack == 0)
                        _propertyController.GetContextController().WhitelistContext<WeaponHitContextBase>();
                }
            }
        }

        private bool _destroyed = false;
        public float CurrentFireRate { get; private set; }
        public float CurrentBurstDelay { get; private set; }
        public float BaseFireRate { get; private set; }

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
                BaseFireRate = 1f / Math.Max(Gun!.m_archeType.ShotDelay(), CustomWeaponData.MinShotDelay);
                _lastFireRate = BaseFireRate;
                CurrentFireRate = BaseFireRate;
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
            if (!IsLocal) return;
            // Bots need full behavior but bots are pain and use different functions so idc for now
            Clear();
            _propertyController.ChangeToSyncContexts();
            IsLocal = false;
            Register(CustomWeaponManager.GetCustomGunData(Weapon.ArchetypeID));
        }

        private void Update()
        {
            if (OwnerSet)
                Invoke(StaticContext<WeaponUpdateContext>.Instance);
        }

        private void OnEnable()
        {
            if (OwnerSet)
                Invoke(StaticContext<WeaponEnableContext>.Instance);
        }

        private void OnDisable()
        {
            if (OwnerSet)
                Invoke(StaticContext<WeaponDisableContext>.Instance);
        }

        private void OnDestroy()
        {
            _destroyed = true;
            Invoke(StaticContext<WeaponClearContext>.Instance);
        }

        [HideFromIl2Cpp]
        public TContext Invoke<TContext>(TContext context) where TContext : IWeaponContext
        {
            if (!RunHitTriggers && context is WeaponHitContextBase)
                return context;

            return _propertyController.Invoke(context);
        }

        [HideFromIl2Cpp]
        public void Register(CustomWeaponData? data = null)
        {
            if (enabled || _destroyed) return; // Don't want to register data twice
            enabled = true;

            if (data == null)
            {
                data = IsGun ? CustomWeaponManager.GetCustomGunData(Weapon.ArchetypeID) : CustomWeaponManager.GetCustomMeleeData(Weapon.MeleeArchetypeData.persistentID);
                if (data == null) return;
            }

            // If called by Activate(), i.e. without data, need to ensure it gets set to sync when applicable
            if (IsLocal && Weapon.TryCast<BulletWeaponSynced>() != null)
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
            _ownerPtr = IntPtr.Zero;
            enabled = false;
            CurrentFireRate = BaseFireRate;
            CurrentBurstDelay = _burstDelay;
            Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        [HideFromIl2Cpp]
        internal void ActivateNode(PropertyNode node) => _propertyController.SetActive(node, true);
        [HideFromIl2Cpp]
        internal void DeactivateNode(PropertyNode node) => _propertyController.SetActive(node, false);

        [HideFromIl2Cpp]
        public bool HasTrait<T>() where T : Trait => _propertyController.HasTrait<T>();
        [HideFromIl2Cpp]
        public T? GetTrait<T>() where T : Trait => _propertyController.GetTrait<T>();
        [HideFromIl2Cpp]
        public bool TryGetTrait<T>([MaybeNullWhen(false)] out T trait) where T : Trait => _propertyController.TryGetTrait(out trait);

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

        public void RefreshArchetypeCache()
        {
            if (IsGun)
            {
                BaseFireRate = 1f / Math.Max(Gun!.m_archeType.ShotDelay(), CustomWeaponData.MinShotDelay);
                _burstDelay = Gun.m_archeType.BurstDelay();
                UpdateStoredFireRate();
            }
        }

        public void RefreshSoundDelay()
        {
            Weapon.Sound.SetRTPCValue(GAME_PARAMETERS.FIREDELAY, 1f / CurrentFireRate);
        }

        public void NotifyShotFired() => _lastFireTime = Clock.Time;

        public void UpdateStoredFireRate()
        {
            BulletWeaponArchetype bwa = Gun!.m_archeType;
            _lastFireRate = CurrentFireRate;
            _lastShotTimer = bwa.m_nextShotTimer;
            _lastBurstTimer = bwa.m_nextBurstTimer;

            float newFireRate = Invoke(new WeaponFireRateContext(BaseFireRate)).Value;

            if (CurrentFireRate != newFireRate)
            {
                CurrentFireRate = Math.Clamp(newFireRate, 0.001f, CustomWeaponData.MaxFireRate);
                CurrentBurstDelay = _burstDelay * BaseFireRate / CurrentFireRate;
                RefreshSoundDelay();
            }
        }

        public void ModifyFireRate() {
            BulletWeaponArchetype bwa = Gun!.m_archeType;
            bwa.m_nextShotTimer = _lastFireTime + 1f / CurrentFireRate;
            if (bwa.BurstIsDone())
                bwa.m_nextBurstTimer = Math.Max(_lastFireTime + CurrentBurstDelay, bwa.m_nextShotTimer);
        }

        public void ModifyFireRateSynced(BulletWeaponSynced synced)
        {
            synced.m_lastFireTime = _lastFireTime + 1f / CurrentFireRate - Weapon.ArchetypeData.ShotDelay;
        }
    }
}
