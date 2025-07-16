using EWC.CustomWeapon.WeaponContext;
using System.Collections.Generic;
using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils.Log;
using System.Diagnostics.CodeAnalysis;
using EWC.Utils.Extensions;

namespace EWC.CustomWeapon.Properties
{
    internal sealed class PropertyController
    {
        private PropertyNode _root = null!;
        private readonly ContextController _contextController;
        private readonly List<WeaponPropertyBase> _properties = new();
        private readonly LinkedList<PropertyNode> _overrideStack = new();
        private readonly Dictionary<Type, Trait> _activeTraits = new();
        private readonly Dictionary<uint, PropertyRef> _idToProperty = new();
        private readonly List<ITriggerCallbackSync> _syncList = new(1);

        // Used when moving to an override root
        private static readonly List<WeaponPropertyBase> s_subtreePropCache = new();
        private static readonly HashSet<WeaponPropertyBase> s_subtreeKeepCache = new();

        public PropertyController(bool isGun, bool isLocal)
        {
            _contextController = new(isGun, isLocal);
        }

        public TContext Invoke<TContext>(TContext context) where TContext : IWeaponContext => _contextController.Invoke(context);

        // Does not go through the standard invoke checks/pipeline like context controller.
        // Should ONLY use for contexts ran once as a part of initialization, e.g. when the weapon owner is set.
        public TContext InvokeAll<TContext>(TContext context) where TContext : IWeaponContext
        {
            foreach (var property in _properties)
            {
                if (property.IsProperty<TContext>(out var tProperty))
                    tProperty.Invoke(context);
            }
            return context;
        }

        public void Init(CustomWeaponComponent cwc, PropertyList? baseList)
        {
            if (baseList == null) return;

            _root = CreateTree(cwc, baseList);
            RemoveInvalidProperties(_root, cwc.IsGun);
            RegisterPropertyIDs();
            Activate(_root);
            RegisterSyncProperties();
        }

        private PropertyNode CreateTree(CustomWeaponComponent cwc, PropertyList list, PropertyNode? parent = null)
        {
            PropertyNode curNode = new(list, parent);
            list.SetCWC(cwc);
            foreach (WeaponPropertyBase property in list.Properties)
            {
                _properties.Add(property);

                if (property is TempProperties tempProperties)
                {
                    if (!tempProperties.Properties.Empty)
                        tempProperties.Node = CreateTree(cwc, tempProperties.Properties, curNode);
                }
                else if (property is IReferenceHolder refHolder)
                    refHolder.Properties.SetCWC(cwc);
            }
            return curNode;
        }

        private void RemoveInvalidProperties(PropertyNode? node, bool isGun)
        {
            if (node == null) return;

            for (int i = node.List.Properties.Count - 1; i >= 0; i--)
            {
                WeaponPropertyBase property = node.List.Properties[i];
                if (!property.ValidProperty())
                {
                    EWCLogger.Warning($"Cannot add {property.GetType().Name} to a {(isGun ? "gun" : "melee")}!");

                    node.List.Properties.Remove(property);
                    Type type = property.GetType();
                    if (node.List.Traits?.TryGetValue(type, out var trait) == true && trait == property)
                        node.List.Traits.Remove(type);
                }
            }

            foreach (var child in node.Children)
                RemoveInvalidProperties(child, isGun);
        }

        private void RegisterSyncProperties()
        {
            foreach (var property in _properties)
            {
                if (property is ITriggerCallbackSync syncProperty)
                {
                    syncProperty.SyncID = (ushort)_syncList.Count;
                    _syncList.Add(syncProperty);
                }
            }
        }

        private void RegisterPropertyIDs()
        {
            if (_root == null) return;

            CachePropertyIDs();
            SetReferenceProperties(_root);
        }

        private void CachePropertyIDs()
        {
            foreach (var property in _properties)
            {
                if (property.ID != 0)
                {
                    if (!_idToProperty.TryAdd(property.ID, property.Reference))
                        EWCLogger.Warning("Duplicate property ID detected: " + property.ID);
                }
            }
        }

        private void SetReferenceProperties(PropertyNode node)
        {
            foreach (var refHolder in node.List.ReferenceHolders.OrEmptyIfNull())
            {
                foreach (var property in refHolder.Properties.ReferenceProperties.OrEmptyIfNull())
                {
                    if (_idToProperty.TryGetValue(property.ReferenceID, out var refProperty))
                    {
                        property.SetReference(refProperty);
                        refHolder.OnReferenceSet(refProperty.Property);
                    }
                    else if (property.ReferenceID != 0)
                        EWCLogger.Error($"Unable to find property with ID {property.ReferenceID}!");
                }
            }

            foreach (var child in node.Children)
                SetReferenceProperties(child);
        }

        public void Clear()
        {
            _properties.Clear();
            _contextController.Clear();
            _overrideStack.Clear();
            _activeTraits.Clear();
            _idToProperty.Clear();
            _syncList.Clear();
        }

        public ContextController GetContextController() => _contextController;
        public bool HasTempProperties() => _root.Children.Count > 0;
        public bool HasTrait<T>() where T : Trait => _activeTraits.ContainsKey(typeof(T));
        public T? GetTrait<T>() where T : Trait => _activeTraits.TryGetValueAs<Type, Trait, T>(typeof(T), out T? trait) ? trait : null;
        public bool TryGetTrait<T>([MaybeNullWhen(false)] out T trait) where T : Trait => _activeTraits.TryGetValueAs(typeof(T), out trait);

        internal ITriggerCallbackSync GetTriggerSync(ushort id)
        {
            if (_syncList.Count <= id)
            {
                EWCLogger.Error("Synced property with ID " + id + " was requested but doesn't exist. Filling with blank properties.");
                while (_syncList.Count <= id)
                    _syncList.Add(TriggerCallbackSyncDummy.Instance);
            }
            return _syncList[id];
        }

        public void SetActive(PropertyNode node, bool active)
        {
            if (active)
                Activate(node);
            else
                Deactivate(node);
        }

        private void Activate(PropertyNode node)
        {
            if (node.Active) return;
            node.Active = true;

            if (node.List.Override)
            {
                if (node.Enabled)
                    UpdateRoot(node);
                else
                    _overrideStack.AddBefore(_overrideStack.Last!, node);
                return;
            }

            if (!node.Enabled) return;

            foreach (var curr in node.List.Properties)
            {
                // Convert ReferenceProperty to property
                var property = curr.Reference.Property;
                if (property.Reference.RefCount++ != 0) continue;

                if (property is Trait trait && !_activeTraits.TryAdd(property.GetType(), trait)) continue;
                
                if (property.IsProperty<WeaponSetupContext>(out var setup))
                    setup.Invoke(StaticContext<WeaponSetupContext>.Instance);

                _contextController.Register(property);
            }
        }

        private void Deactivate(PropertyNode node)
        {
            if (!node.Active) return;
            node.Active = false;

            if (node.List.Override)
            {
                _overrideStack.Remove(node);
                if (node.Enabled)
                    UpdateRoot();
                return;
            }

            if (!node.Enabled) return;

            foreach (var curr in node.List.Properties)
            {
                // Convert ReferenceProperty to property
                var property = curr.Reference.Property;
                if (--property.Reference.RefCount != 0) continue;

                Type type = property.GetType();
                if (property is Trait trait && (!_activeTraits.TryGetValue(type, out var active) || active != trait))
                    continue;
                else
                    _activeTraits.Remove(type);

                if (property.IsProperty<WeaponClearContext>(out var clear))
                    clear.Invoke(StaticContext<WeaponClearContext>.Instance);

                _contextController.Unregister(property);
            }
        }

        private void UpdateRoot(PropertyNode? node = null)
        {
            if (node != null)
                _overrideStack.AddLast(node);
            else
                node = _overrideStack.Last?.Value;

            foreach (var property in _contextController.Properties)
                property.Reference.RefCount = 0;

            node ??= _root;

            void SetSubtree(PropertyNode curr, bool enabled = false)
            {
                if (curr == node)
                {
                    // Keep the TempProperties that had the override in the subtree
                    if (curr.List.Owner != null && curr.List.Owner.Reference.RefCount++ == 0)
                        s_subtreePropCache.Add(curr.List.Owner);
                    enabled = true;
                }
                curr.Enabled = enabled;

                // Add all properties from active nodes
                if (enabled && curr.Active)
                {
                    foreach (var property in curr.List.Properties)
                    {
                        if (property.Reference.RefCount++ == 0) // Only add a single property once
                            s_subtreePropCache.Add(property.Reference.Property);
                    }
                }

                foreach (var child in curr.Children)
                    SetSubtree(child, enabled);
            }

            SetSubtree(_root);

            // Clear any properties that were not preserved
            foreach (var property in _contextController.Properties)
            {
                if (property.Reference.RefCount == 0 && property is IWeaponProperty<WeaponClearContext> clearProp)
                    clearProp.Invoke(StaticContext<WeaponClearContext>.Instance);
                else
                    s_subtreeKeepCache.Add(property);
            }

            _contextController.Clear();
            _activeTraits.Clear();
            // Add the properties back in from the cache
            foreach (var property in s_subtreePropCache)
            {
                if (!s_subtreeKeepCache.Contains(property) && property is IWeaponProperty<WeaponSetupContext> setupProp)
                    setupProp.Invoke(StaticContext<WeaponSetupContext>.Instance);
                if (property is Trait trait)
                    _activeTraits.TryAdd(property.GetType(), trait);
                _contextController.Register(property);
            }

            s_subtreePropCache.Clear();
            s_subtreeKeepCache.Clear();
        }
    }
}
