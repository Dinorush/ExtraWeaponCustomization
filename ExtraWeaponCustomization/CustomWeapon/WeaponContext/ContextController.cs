using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EWC.CustomWeapon.WeaponContext
{
    public sealed class ContextController
    {
        private readonly Dictionary<Type, IContextList> _allContextLists = new();

        public ContextController(ContextController contextController)
        {
            foreach (IContextList list in contextController._allContextLists.Values)
                list.CopyTo(this);
        }

        public ContextController(bool isGun)
        {
            if (isGun)
                RegisterGunContexts();
            else
                RegisterMeleeContexts();
        }

        private interface IContextList
        {
            bool Add(IWeaponProperty property);
            bool Remove(IWeaponProperty property);
            void Clear();
            IContextList? CopyTo(ContextController manager);
            void Invoke(IWeaponContext context, List<Exception> exceptions);
        }

        private sealed class ContextList<TContext> : IContextList where TContext : IWeaponContext
        {
            private readonly List<IWeaponProperty<TContext>> _entries;
            private readonly IContextList? _baseContextList;

            internal ContextList(ContextController manager, IContextList? baseList = null)
            {
                _entries = new();

                _baseContextList = baseList;
                if (_baseContextList == null)
                {
                    Type? baseType = typeof(TContext).BaseType;
                    if (baseType != null && manager._allContextLists.ContainsKey(baseType))
                        _baseContextList = manager._allContextLists[baseType];
                }
            }

            public bool Add(IWeaponProperty property)
            {
                if (property is not IWeaponProperty<TContext> contextedProperty)
                    return false;

                if (_entries.Contains(contextedProperty))
                    return false;

                if (property is Trait && _entries.Any(containedProperty => containedProperty.GetType() == contextedProperty.GetType()))
                    return false;

                _entries.Add(contextedProperty);
                return true;
            }

            public bool Remove(IWeaponProperty property)
            {
                return property is IWeaponProperty<TContext> contextedProperty && _entries.Remove(contextedProperty);
            }

            public void Clear()
            {
                _entries.Clear();
            }

            public IContextList? CopyTo(ContextController manager)
            {
                Type type = typeof(TContext);
                if (manager._allContextLists.TryGetValue(type, out var context)) return context;

                IContextList? baseList = _baseContextList?.CopyTo(manager);

                ContextList<TContext> copy = new(manager, baseList);
                copy._entries.AddRange(_entries);
                manager._allContextLists.Add(type, copy);
                return copy;
            }

            public void Invoke(TContext context, List<Exception> exceptions)
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    IWeaponProperty<TContext> property = _entries[i];
                    try
                    {
                        property.Invoke(context);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }

                _baseContextList?.Invoke(context, exceptions);
            }

            void IContextList.Invoke(IWeaponContext context, List<Exception> exceptions)
            {
                if (context is not TContext tContext)
                    return;

                Invoke(tContext, exceptions);
            }
        }

        public void Register(IWeaponProperty property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            foreach (IContextList contextList in _allContextLists.Values)
            {
                contextList.Add(property);
            }
        }

        public void Unregister(IWeaponProperty property)
        {
            foreach (IContextList contextList in _allContextLists.Values)
            {
                contextList.Remove(property);
            }
        }

        public void Clear()
        {
            foreach (IContextList contextList in _allContextLists.Values)
            {
                contextList.Clear();
            }
        }

        internal TContext Invoke<TContext>(TContext context) where TContext : IWeaponContext
        {
            if (!_allContextLists.TryGetValue(typeof(TContext), out IContextList? contextList)) return context;

            List<Exception> exceptions = new();
            contextList.Invoke(context, exceptions);

            foreach (var exception in exceptions)
                EWCLogger.Error(exception.Message);
            return context;
        }

        private ContextList<TContext> RegisterContext<TContext>(IContextList? baseList = null) where TContext : IWeaponContext
        {
            var newList = new ContextList<TContext>(this, baseList);
            _allContextLists.TryAdd(typeof(TContext), newList);
            return newList;
        }

        private void RegisterGunContexts()
        {
            var triggerList = RegisterContext<WeaponTriggerContext>();
            RegisterContext<WeaponDamageTypeContext>();
            RegisterContext<WeaponAimContext>();
            RegisterContext<WeaponAimEndContext>();
            RegisterContext<WeaponPostKillContext>(triggerList);
            RegisterContext<WeaponPostFireContext>();
            RegisterContext<WeaponPreFireContext>();
            RegisterContext<WeaponHitContext>(triggerList);
            RegisterContext<WeaponPreHitDamageableContext>(triggerList);
            RegisterContext<WeaponHitDamageableContext>(triggerList);
            RegisterContext<WeaponPreReloadContext>();
            RegisterContext<WeaponPostReloadContext>();
            RegisterContext<WeaponReloadStartContext>();
            RegisterContext<WeaponWieldContext>();
            RegisterContext<WeaponUnWieldContext>();

            RegisterContext<WeaponArmorContext>();
            RegisterContext<WeaponBackstabContext>();
            RegisterContext<WeaponFireCanceledContext>();
            RegisterContext<WeaponDamageContext>();
            RegisterContext<WeaponPostAmmoInitContext>();
            RegisterContext<WeaponPreAmmoPackContext>();
            RegisterContext<WeaponPostAmmoPackContext>();
            RegisterContext<WeaponPreAmmoUIContext>();
            RegisterContext<WeaponFireRateContext>();
            RegisterContext<WeaponPierceContext>();
            RegisterContext<WeaponFireCancelContext>();
            RegisterContext<WeaponPreStartFireContext>();
            RegisterContext<WeaponPostStartFireContext>();
            RegisterContext<WeaponPreRayContext>();
            RegisterContext<WeaponCancelRayContext>();
            RegisterContext<WeaponPostRayContext>();
            RegisterContext<WeaponPostStopFiringContext>();
            RegisterContext<WeaponRecoilContext>();
            RegisterContext<WeaponStealthUpdateContext>();

            RegisterContext<WeaponClearContext>();
            RegisterContext<WeaponSetupContext>();
            RegisterContext<WeaponOwnerSetContext>();
            RegisterContext<WeaponUpdateContext>();
            RegisterContext<WeaponEnableContext>();
            RegisterContext<WeaponDisableContext>();
        }

        private void RegisterMeleeContexts()
        {
            var triggerList = RegisterContext<WeaponTriggerContext>();
            RegisterContext<WeaponDamageTypeContext>();
            RegisterContext<WeaponPostKillContext>(triggerList);
            RegisterContext<WeaponPostFireContext>();
            RegisterContext<WeaponPreFireContext>();
            RegisterContext<WeaponHitContext>(triggerList);
            RegisterContext<WeaponPreHitDamageableContext>(triggerList);
            RegisterContext<WeaponHitDamageableContext>(triggerList);
            RegisterContext<WeaponWieldContext>();

            RegisterContext<WeaponArmorContext>();
            RegisterContext<WeaponBackstabContext>();
            RegisterContext<WeaponDamageContext>();
            RegisterContext<WeaponFireRateContext>();
            RegisterContext<WeaponStealthUpdateContext>();

            RegisterContext<WeaponClearContext>();
            RegisterContext<WeaponSetupContext>();
        }

        internal void ChangeToSyncContexts()
        {
            _allContextLists.Clear();
            RegisterContext<WeaponStealthUpdateContext>();
            RegisterContext<WeaponFireRateContext>();
            RegisterContext<WeaponPreFireContextSync>();
            RegisterContext<WeaponPostFireContextSync>();
        }
    }
}