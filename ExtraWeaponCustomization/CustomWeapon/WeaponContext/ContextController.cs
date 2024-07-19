using ExtraWeaponCustomization.CustomWeapon.Properties;
using ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts;
using ExtraWeaponCustomization.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext
{
    public sealed class ContextController
    {
        private readonly Dictionary<Type, IContextList> _allContextLists = new();

        public ContextController()
        {
            RegisterAllContexts();
        }

        private interface IContextList
        {
            bool Add(IWeaponProperty property);
            bool Remove(IWeaponProperty property);
            void Clear();
            void Invoke(IWeaponContext context, List<Exception> exceptions);
        }

        private sealed class ContextList<TContext> : IContextList where TContext : IWeaponContext
        {
            private readonly List<IWeaponProperty<TContext>> _entries;
            private readonly IContextList? _baseContextList;
            private readonly ContextController _manager;

            internal ContextList(ContextController manager)
            {
                _entries = new();

                Type? baseType = typeof(TContext).BaseType;
                if (baseType != null && manager._allContextLists.ContainsKey(baseType))
                    _baseContextList = manager._allContextLists[baseType];

                _manager = manager;
                _manager._allContextLists.Add(typeof(TContext), this);
            }

            public bool Add(IWeaponProperty property)
            {
                if (property is not IWeaponProperty<TContext> contextedProperty)
                    return false;

                if (_entries.Contains(contextedProperty))
                    return false;

                if (!property.AllowStack && _entries.Any(containedProperty => containedProperty.GetType() == contextedProperty.GetType()))
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

            public void Invoke(TContext context, List<Exception> exceptions)
            {
                foreach (IWeaponProperty<TContext> property in _entries)
                {
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

        internal void Invoke<TContext>(TContext context) where TContext : IWeaponContext
        {
            if (!_allContextLists.TryGetValue(typeof(TContext), out IContextList? contextList)) return;

            List<Exception> exceptions = new();
            contextList.Invoke(context, exceptions);

            foreach (var exception in exceptions)
                EWCLogger.Error(exception.Message);
        }

        internal void RegisterContext<TContext>() where TContext : IWeaponContext
        {
            _allContextLists.TryAdd(typeof(TContext), new ContextList<TContext>(this));
        }

        internal void RegisterAllContexts()
        {
            RegisterContext<WeaponTriggerContext>();
            RegisterContext<WeaponPostKillContext>();
            RegisterContext<WeaponPostFireContext>();
            RegisterContext<WeaponPreHitContext>();
            RegisterContext<WeaponPreHitEnemyContext>();
            RegisterContext<WeaponPostReloadContext>();
            RegisterContext<WeaponWieldContext>();

            RegisterContext<WeaponArmorContext>();
            RegisterContext<WeaponCancelFireContext>();
            RegisterContext<WeaponDamageContext>();
            RegisterContext<WeaponPostAmmoInitContext>();
            RegisterContext<WeaponPreAmmoPackContext>();
            RegisterContext<WeaponPostAmmoPackContext>();
            RegisterContext<WeaponPreAmmoUIContext>();
            RegisterContext<WeaponFireRateContext>();
            RegisterContext<WeaponFireRateSetContext>();
            RegisterContext<WeaponPreFireContext>();
            RegisterContext<WeaponPreStartFireContext>();
            RegisterContext<WeaponPostStartFireContext>();
            RegisterContext<WeaponPreRayContext>();
            RegisterContext<WeaponPostStopFiringContext>();
            RegisterContext<WeaponRecoilContext>();

            RegisterContext<WeaponPostSetupContext>();
        }
    }
}