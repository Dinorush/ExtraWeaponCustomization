using EWC.Attributes;
using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.ComponentWrapper.OwnerComps;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.CustomShot;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Effects.Debuff;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.CustomWeapon.WeaponContext.Contexts.Base;
using EWC.Utils;
using EWC.Utils.Extensions;
using Gear;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace EWC.CustomWeapon
{
    public class CustomWeaponComponent : MonoBehaviour
    {
        public readonly IWeaponComp Weapon;
        [HideFromIl2Cpp]
        public IOwnerComp Owner { get; private set; }

        public readonly CustomShotComponent ShotComponent;
        public readonly SpreadController SpreadController;

        private readonly PropertyController _propertyController;

        // Used to prevent hit callbacks from firing. For some effects to prevent infinite recursion.
        private int _ignoreHitStack = 0;
        public bool RunHitTriggers
        {
            get { return _ignoreHitStack == 0; }
            set 
            {
                if (!value)
                {
                    if (_ignoreHitStack++ == 0)
                    {
                        GetContextController().BlacklistContext<WeaponHitContextBase>();
                        GetContextController().BlacklistContext<WeaponShotEndContext>();
                    }
                }
                else if (value && _ignoreHitStack > 0)
                {
                    if (--_ignoreHitStack == 0)
                    {
                        GetContextController().WhitelistContext<WeaponHitContextBase>();
                        GetContextController().BlacklistContext<WeaponShotEndContext>();
                    }
                }
            }
        }

        [HideFromIl2Cpp]
        public HashSet<uint> DebuffIDs { get; private set; } = DebuffGroup.DefaultGroupList;

        // Used to update delayed callbacks before invocations happen.
        private readonly LinkedList<DelayedCallback> _timeSensitiveCallbacks;
        private float _lastInvokeTime = 0f;

        protected bool _destroyed = false;

        public CustomWeaponComponent(IntPtr value) : base(value)
        {
            var item = GetComponent<ItemEquippable>();
            if (item != null)
            {
                var owner = item.Owner;
                if (item.TryCastOut<MeleeWeaponFirstPerson>(out var melee))
                {
                    Weapon = new MeleeComp(melee);
                    Owner = new LocalOwnerComp(owner, item.MuzzleAlign);
                }
                else if (item.TryCastOut<BulletWeaponSynced>(out var gunSynced))
                {
                    Weapon = new SyncedGunComp(gunSynced);
                    Owner = new SyncedOwnerComp(owner, item.MuzzleAlign);
                }
                else if (item.TryCastOut<BulletWeapon>(out var gun))
                {
                    Weapon = new LocalGunComp(gun);
                    Owner = new LocalOwnerComp(owner, item.MuzzleAlign);
                }
                else if (item.TryCastOut<SentryGunFirstPerson>(out var holder))
                {
                    Weapon = new SentryHolderComp(holder);
                    Owner = new LocalOwnerComp(owner, item.MuzzleAlign);
                }
                else if (item.TryCastOut<SentryGunInstance>(out var sentry))
                {
                    Weapon = new SentryGunComp(sentry);
                    Owner = new SentryOwnerComp(sentry);
                }
                else
                    throw new ArgumentException("Custom Weapon Component was added to a non-melee/gun/sentry.");
            }
            else
                throw new ArgumentException("Custom Weapon Component was added to a non-melee/gun/sentry.");

            _propertyController = new(Owner.Type, Weapon.Type);
            ShotComponent = new(this);
            SpreadController = new(Owner.Type, Weapon.Type);
            _timeSensitiveCallbacks = new();
            enabled = false;
        }

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomWeaponComponent>();
        }

        public virtual void OnUnWield()
        {
            SpreadController.Active = false;
            Invoke(StaticContext<WeaponUnWieldContext>.Instance);
        }

        public virtual void OnWield()
        {
            SpreadController.Active = true;
            Invoke(StaticContext<WeaponWieldContext>.Instance);
        }

        private void Update()
        {
            Invoke(StaticContext<WeaponUpdateContext>.Instance);
        }

        private void OnEnable() {}

        private void OnDisable() {}

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
        private TContext InvokeAll<TContext>(TContext context) where TContext : IWeaponContext => _propertyController.InvokeAll(context);

        [HideFromIl2Cpp]
        public void Register(CustomWeaponData? data = null)
        {
            if (enabled || _destroyed) return; // Don't want to register data twice
            enabled = true;

            if (data == null)
            {
               if (!CustomDataManager.TryGetCustomData(Weapon, out data))
                    return;
            }

            _propertyController.Init(this, data.Properties.Clone());
            DebuffIDs = data.DebuffIDs.IDs;
            DebuffIDs.Add(DebuffManager.DefaultGroup);
            InvokeAll(StaticContext<WeaponCreatedContext>.Instance);
            Invoke(new WeaponInitContext(Owner, Weapon));
            TriggerManager.RunQueuedReceives(this);
        }

        public virtual void Clear()
        {
            if (_destroyed)
            {
                foreach (var callback in _timeSensitiveCallbacks)
                    callback.Cancel();
                _timeSensitiveCallbacks.Clear();
            }
            Invoke(StaticContext<WeaponClearContext>.Instance);
            _propertyController.Clear();
            SpreadController.Reset();
            DebuffIDs = DebuffGroup.DefaultGroupList;
            enabled = false;
        }

        public void RefreshOwner()
        {
            var item = Weapon.Component;
            var owner = item.Owner;
            if (owner == null || owner.Owner == null) return;

            if (Owner.IsType(Enums.OwnerType.Local))
                Owner = new LocalOwnerComp(owner, item.MuzzleAlign);
            else if (Owner.IsType(Enums.OwnerType.Sentry))
                Owner = new SentryOwnerComp(((SentryGunComp)Weapon).Value);
            else
                Owner = new SyncedOwnerComp(owner, item.MuzzleAlign);
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
        public bool TryGetReferenceHolder(uint id, [MaybeNullWhen(false)] out PropertyRef propertyRef) => _propertyController.TryGetReferenceHolder(id, out propertyRef);
        [HideFromIl2Cpp]
        public bool TryGetReference(uint id, [MaybeNullWhen(false)] out WeaponPropertyBase property) => _propertyController.TryGetReference(id, out property);

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
    }
}
