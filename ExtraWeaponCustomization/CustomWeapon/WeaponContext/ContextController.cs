using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Properties;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext.Attributes;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EWC.CustomWeapon.WeaponContext
{
    public sealed class ContextController
    {
        private static readonly Type[] s_contextTypes;
        private static readonly Dictionary<Type, Type> s_parentContextTypes = new();
        private static Dictionary<(OwnerType, WeaponType), List<Type>> s_cachedContextTypes = new();

        static ContextController()
        {
            s_contextTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => 
                t.IsAssignableTo(typeof(IWeaponContext)) &&
                (!t.IsAbstract || t.GetCustomAttribute<AllowAbstractAttribute>() != null))
                .ToArray();

            foreach (var type in s_contextTypes)
            {
                var parent = type.GetCustomAttribute<ParentContextAttribute>()?.Parent;
                if (parent != null && type != parent)
                    s_parentContextTypes.Add(type, parent);
            }
        }

        private readonly Dictionary<Type, IContextList> _contextLists = new();
        private readonly List<Type> _blacklist;
        private readonly HashSet<WeaponPropertyBase> _properties;

        public ContextController(ContextController contextController)
        {
            foreach (IContextList list in contextController._contextLists.Values)
                list.CopyTo(this);
            _blacklist = new(contextController._blacklist);
            _properties = new(contextController._properties);
        }

        public ContextController(OwnerType ownerType, WeaponType weaponType)
        {
            RegisterContexts(ownerType, weaponType);

            _blacklist = new();
            _properties = new();
        }

        private interface IContextList
        {
            public IContextList? ParentContextList { get; set; }
            bool Add(IWeaponProperty property);
            bool Remove(IWeaponProperty property);
            void Clear();
            IContextList? CopyTo(ContextController manager);
            void Invoke(IWeaponContext context, List<Exception> exceptions);
        }

        private sealed class ContextList<TContext> : IContextList where TContext : IWeaponContext
        {
            private readonly List<IWeaponProperty<TContext>> _entries;
            public IContextList? ParentContextList { get; set; }

            public ContextList()
            {
                _entries = new();
            }

            public bool Add(IWeaponProperty property)
            {
                if (!property.IsProperty<TContext>(out var contextedProperty))
                    return false;

                if (property is Trait && _entries.Any(containedProperty => containedProperty.GetType() == contextedProperty.GetType()))
                    return false;

                _entries.Add(contextedProperty);
                return true;
            }

            public bool Remove(IWeaponProperty property)
            {
                return property.IsProperty<TContext>(out var contextedProperty) && _entries.Remove(contextedProperty);
            }

            public void Clear() => _entries.Clear();

            public IContextList? CopyTo(ContextController manager)
            {
                Type type = typeof(TContext);
                if (manager._contextLists.TryGetValue(type, out var context)) return context;

                IContextList? parentList = ParentContextList?.CopyTo(manager);

                ContextList<TContext> copy = new();
                copy.ParentContextList = parentList;
                copy._entries.AddRange(_entries);
                manager._contextLists.Add(type, copy);
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

                ParentContextList?.Invoke(context, exceptions);
            }

            void IContextList.Invoke(IWeaponContext context, List<Exception> exceptions)
            {
                if (context is not TContext tContext)
                    return;

                Invoke(tContext, exceptions);
            }
        }

        public void Register(WeaponPropertyBase property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (!_properties.Add(property)) return;

            foreach (IContextList contextList in _contextLists.Values)
            {
                contextList.Add(property);
            }
        }

        public void Unregister(WeaponPropertyBase property)
        {
            if (!_properties.Remove(property)) return;

            foreach (IContextList contextList in _contextLists.Values)
            {
                contextList.Remove(property);
            }
        }

        public IReadOnlySet<WeaponPropertyBase> Properties => _properties;

        public void Clear()
        {
            _properties.Clear();
            foreach (IContextList contextList in _contextLists.Values)
            {
                contextList.Clear();
            }
        }

        public void BlacklistContext<T>() where T : IWeaponContext => _blacklist.Add(typeof(T));
        public void WhitelistContext<T>() where T : IWeaponContext => _blacklist.Remove(typeof(T));

        public TContext Invoke<TContext>(TContext context) where TContext : IWeaponContext
        {
            Type type = typeof(TContext);
            if (_blacklist.Count > 0 && _blacklist.Any(exclude => type.IsAssignableTo(exclude))) return context;
            if (!_contextLists.TryGetValue(type, out IContextList? contextList)) return context;

            List<Exception> exceptions = new();
            contextList.Invoke(context, exceptions);

            foreach (var exception in exceptions)
                EWCLogger.Error(exception.Message);
            return context;
        }

        private ContextList<TContext> RegisterContext<TContext>(IContextList? baseList = null) where TContext : IWeaponContext
        {
            var newList = new ContextList<TContext>();
            newList.ParentContextList = baseList;
            _contextLists.TryAdd(typeof(TContext), newList);
            return newList;
        }

        private void RegisterContexts(OwnerType ownerType, WeaponType weaponType)
        {
            if (!s_cachedContextTypes.TryGetValue((ownerType, weaponType), out var contextTypes))
            {
                contextTypes = new();
                foreach (var type in s_contextTypes)
                {
                    var requiredTypes = type.GetCustomAttribute<RequireTypeAttribute>();
                    if (requiredTypes == null || requiredTypes.IsValid(ownerType, weaponType))
                        contextTypes.Add(type);
                }
            }

            foreach (var type in contextTypes)
            {
                var listType = typeof(ContextList<>).MakeGenericType(type);
                var list = (IContextList)Activator.CreateInstance(listType)!;
                _contextLists.TryAdd(type, list);
            }

            foreach ((var type, var parentType) in s_parentContextTypes)
            {
                if (_contextLists.TryGetValue(type, out var list) && _contextLists.TryGetValue(parentType, out var parentList))
                    list.ParentContextList = parentList;
            }
        }

        private void RegisterGunContexts()
        {
            // Only contexts that aren't direct descendants of WeaponTriggerContext need to have it passed directly,
            // but why not micro-optimize?
            var triggerList = RegisterContext<WeaponTriggerContext>();
            RegisterContext<WeaponAimContext>(triggerList);
            RegisterContext<WeaponAimEndContext>(triggerList);
            RegisterContext<WeaponAmmoContext>(triggerList);
            RegisterContext<WeaponPostKillContext>(triggerList);
            RegisterContext<WeaponPostStaggerContext>(triggerList);
            RegisterContext<WeaponPostFireContext>(triggerList);
            RegisterContext<WeaponPreFireContext>(triggerList);
            RegisterContext<WeaponHitContext>(triggerList);
            RegisterContext<WeaponPreHitDamageableContext>(triggerList);
            RegisterContext<WeaponHitDamageableContext>(triggerList);
            RegisterContext<WeaponShotEndContext>(triggerList);
            RegisterContext<WeaponDamageTakenContext>(triggerList);
            RegisterContext<WeaponHealthContext>(triggerList);
            RegisterContext<WeaponPostReloadContext>(triggerList);
            RegisterContext<WeaponReloadStartContext>(triggerList);
            RegisterContext<WeaponUnWieldContext>(triggerList);
            RegisterContext<WeaponWieldContext>(triggerList);
            RegisterContext<WeaponCrouchContext>(triggerList);
            RegisterContext<WeaponCrouchEndContext>(triggerList);
            RegisterContext<WeaponSprintContext>(triggerList);
            RegisterContext<WeaponSprintEndContext>(triggerList);
            RegisterContext<WeaponJumpContext>(triggerList);
            RegisterContext<WeaponJumpEndContext>(triggerList);
            RegisterContext<WeaponReferenceContext>(triggerList);
            RegisterContext<WeaponInitContext>(triggerList);
            RegisterContext<WeaponPostStartFireContext>(triggerList);
            RegisterContext<WeaponPostStopFiringContext>(triggerList);

            // Standard contexts
            RegisterContext<WeaponArmorContext>();
            RegisterContext<WeaponBackstabContext>();
            RegisterContext<WeaponFireCanceledContext>();
            RegisterContext<WeaponStatContext>();
            RegisterContext<WeaponHitmarkerContext>();
            RegisterContext<WeaponPostAmmoInitContext>();
            RegisterContext<WeaponPreAmmoPackContext>();
            RegisterContext<WeaponPostAmmoPackContext>();
            RegisterContext<WeaponPreAmmoUIContext>();
            RegisterContext<WeaponFireRateContext>();
            RegisterContext<WeaponFireCancelContext>();
            RegisterContext<WeaponPreStartFireContext>();
            RegisterContext<WeaponPreRayContext>();
            RegisterContext<WeaponPreReloadContext>();
            RegisterContext<WeaponRecoilContext>();
            RegisterContext<WeaponShotInitContext>();
            RegisterContext<WeaponShotGroupInitContext>();
            RegisterContext<WeaponStealthUpdateContext>();

            // Component management contexts
            RegisterContext<WeaponClearContext>();
            RegisterContext<WeaponSetupContext>();
            RegisterContext<WeaponUpdateContext>();
        }

        private void RegisterMeleeContexts()
        {
            var triggerList = RegisterContext<WeaponTriggerContext>();
            RegisterContext<WeaponPostKillContext>(triggerList);
            RegisterContext<WeaponPostStaggerContext>(triggerList);
            RegisterContext<WeaponPostFireContext>(triggerList);
            RegisterContext<WeaponPreFireContext>(triggerList);
            RegisterContext<WeaponHitContext>(triggerList);
            RegisterContext<WeaponPreHitDamageableContext>(triggerList);
            RegisterContext<WeaponHitDamageableContext>(triggerList);
            RegisterContext<WeaponShotEndContext>(triggerList);
            RegisterContext<WeaponDamageTakenContext>(triggerList);
            RegisterContext<WeaponHealthContext>(triggerList);
            RegisterContext<WeaponWieldContext>(triggerList);
            RegisterContext<WeaponUnWieldContext>(triggerList);
            RegisterContext<WeaponCrouchContext>(triggerList);
            RegisterContext<WeaponCrouchEndContext>(triggerList);
            RegisterContext<WeaponSprintContext>(triggerList);
            RegisterContext<WeaponSprintEndContext>(triggerList);
            RegisterContext<WeaponJumpContext>(triggerList);
            RegisterContext<WeaponJumpEndContext>(triggerList);
            RegisterContext<WeaponReferenceContext>(triggerList);
            RegisterContext<WeaponInitContext>(triggerList);

            RegisterContext<WeaponArmorContext>();
            RegisterContext<WeaponBackstabContext>();
            RegisterContext<WeaponStatContext>();
            RegisterContext<WeaponHitmarkerContext>();
            RegisterContext<WeaponFireRateContext>();
            RegisterContext<WeaponStealthUpdateContext>();
            RegisterContext<WeaponShotInitContext>();
            RegisterContext<WeaponShotGroupInitContext>();

            RegisterContext<WeaponChargeContext>();

            RegisterContext<WeaponClearContext>();
            RegisterContext<WeaponSetupContext>();
        }

        private void RegisterSyncContexts()
        {
            RegisterContext<WeaponClearContext>();
            RegisterContext<WeaponSetupContext>();
            RegisterContext<WeaponStealthUpdateContext>();
            RegisterContext<WeaponFireRateContext>();
            RegisterContext<WeaponPreFireContextSync>();
            RegisterContext<WeaponPostFireContextSync>();
        }
    }
}