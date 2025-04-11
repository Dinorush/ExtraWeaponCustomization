using AK;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Spread;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Triggers;
using EWC.Utils;
using GameData;
using Gear;
using Il2CppInterop.Runtime.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.CustomWeapon
{
    public sealed class CustomWeaponComponent : MonoBehaviour
    {
        public readonly ItemEquippable Weapon;
        public readonly BulletWeapon? Gun;
        public readonly bool IsGun;
        private ArchetypeDataBlock _archetypeData;
        public ArchetypeDataBlock ArchetypeData
        {
            get => _archetypeData;
            set
            {
                _archetypeData = value;
                Weapon.ArchetypeData = value;
            }
        }
        private BulletWeaponArchetype? _gunArchetype;
        public BulletWeaponArchetype? GunArchetype
        {
            get => _gunArchetype;
            set
            {
                Gun!.m_archeType = value;
                _gunArchetype = value;
                GunFireMode = Weapon.ArchetypeData.FireMode;
                _burstArchetype = GunFireMode == eWeaponFireMode.Burst ? value!.Cast<BWA_Burst>() : null;
            }
        }
        public eWeaponFireMode GunFireMode { get; private set; }
        private BWA_Burst? _burstArchetype;
        public readonly bool IsShotgun;
        public readonly MeleeWeaponFirstPerson? Melee;
        public readonly bool IsMelee;
        public readonly CustomShotComponent? ShotComponent;
        public readonly SpreadController? SpreadController;

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

        // Used to update delayed callbacks before invocations happen.
        private readonly LinkedList<DelayedCallback> _timeSensitiveCallbacks;
        private float _lastInvokeTime = 0f;

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
            IsShotgun = false;
            IsMelee = !IsGun;
            GunFireMode = eWeaponFireMode.Semi;
            _archetypeData = Weapon.ArchetypeData;

            if (IsGun)
            {
                GunArchetype = Gun!.m_archeType;
                BaseFireRate = 1f / Math.Max(GunArchetype!.ShotDelay(), CustomWeaponData.MinShotDelay);
                _lastFireRate = BaseFireRate;
                CurrentFireRate = BaseFireRate;
                _burstDelay = GunArchetype.BurstDelay();
                CurrentBurstDelay = _burstDelay;
                IsLocal = Gun.TryCast<BulletWeaponSynced>() == null;
                IsShotgun = IsLocal ? Weapon.TryCast<Shotgun>() != null : Weapon.TryCast<ShotgunSynced>() != null;
                ShotComponent = new(this);
                if (IsLocal)
                    SpreadController = new();
            }

            _propertyController = new(IsGun, IsLocal);
            _timeSensitiveCallbacks = new();
            enabled = false;
        }

        public bool TryGetBurstArchetype([MaybeNullWhen(false)] out BWA_Burst burstArchetype)
        {
            burstArchetype = _burstArchetype;
            return GunFireMode == eWeaponFireMode.Burst;
        }

        // Only runs on local player!
        public void OwnerInit()
        {
            IntPtr ptr = GunArchetype!.m_owner.Pointer;
            if (ptr == IntPtr.Zero || ptr == _ownerPtr || !enabled) return;

            _ownerPtr = ptr;
            Invoke(StaticContext<WeaponOwnerSetContext>.Instance);
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
            Clear();
        }

        [HideFromIl2Cpp]
        public TContext Invoke<TContext>(TContext context) where TContext : IWeaponContext
        {
            float time = Clock.Time;
            if (_lastInvokeTime != time)
            {
                _lastInvokeTime = time;
                var next = _timeSensitiveCallbacks.First;
                for (var node = next; node != null; node = next)
                {
                    next = node.Next;
                    if (node.Value.CheckEnd())
                        _timeSensitiveCallbacks.Remove(node);
                }
            }

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

            _propertyController.Init(this, data.Properties.Clone());
            if (IsGun && GunArchetype!.m_owner != null)
                OwnerInit();
        }

        public void Clear()
        {
            Invoke(StaticContext<WeaponClearContext>.Instance);
            SpreadController?.Reset();
            _propertyController.Clear();
            _ownerPtr = IntPtr.Zero;
            enabled = false;
            CurrentFireRate = BaseFireRate;
            CurrentBurstDelay = _burstDelay;
            if (!_destroyed)
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

        // Starts a delayed callback. Its end will be checked on Invoke prior to actually running the Invoke.
        [HideFromIl2Cpp]
        public void StartDelayedCallback(DelayedCallback callback, bool checkEnd = false, bool refresh = true)
        {
            if (!_timeSensitiveCallbacks.Contains(callback))
            {
                _timeSensitiveCallbacks.AddLast(callback);
                callback.Start(checkEnd, refresh);
            }
            else
                callback.Start(checkEnd, refresh);
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
                if (TryGetBurstArchetype(out var arch))
                    arch!.m_burstCurrentCount = 0;
                return true;
            }
            return false;
        }

        public void RefreshArchetypeCache()
        {
            if (IsGun)
            {
                BaseFireRate = 1f / Math.Max(GunArchetype!.ShotDelay(), CustomWeaponData.MinShotDelay);
                _burstDelay = Gun!.m_archeType.BurstDelay();
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
            _lastFireRate = CurrentFireRate;
            _lastShotTimer = GunArchetype!.m_nextShotTimer;
            _lastBurstTimer = GunArchetype!.m_nextBurstTimer;

            float newFireRate = Invoke(new WeaponFireRateContext(BaseFireRate)).Value;

            if (CurrentFireRate != newFireRate)
            {
                CurrentFireRate = Math.Clamp(newFireRate, 0.001f, CustomWeaponData.MaxFireRate);
                CurrentBurstDelay = _burstDelay * BaseFireRate / CurrentFireRate;
                RefreshSoundDelay();
            }
        }

        public void ModifyFireRate()
        {
            GunArchetype!.m_nextShotTimer = _lastFireTime + 1f / CurrentFireRate;
            if (GunArchetype.BurstIsDone())
                GunArchetype.m_nextBurstTimer = Math.Max(_lastFireTime + CurrentBurstDelay, GunArchetype.m_nextShotTimer);
        }

        public void ModifyFireRateSynced(BulletWeaponSynced synced)
        {
            synced.m_lastFireTime = _lastFireTime + 1f / CurrentFireRate - ArchetypeData.ShotDelay;
        }
    }
}
