using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.WeaponContext;
using EWC.CustomWeapon.WeaponContext.Contexts;
using EWC.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
            foreach (var property in _properties)
                property.OnReferenceSet();
            Activate(_root);
        }

        private PropertyNode CreateTree(CustomWeaponComponent cwc, PropertyList list, PropertyNode? parent = null)
        {
            PropertyNode curNode = new(list, parent);
            list.SetCWC(cwc);
            for (int i = list.Properties.Count - 1; i >= 0; i--)
            {
                WeaponPropertyBase property = list.Properties[i];
                if (!property.ValidProperty())
                {
                    EWCLogger.Warning($"Cannot add {property.GetType().Name} to a {(cwc.IsGun ? "gun" : "melee")}!");

                    list.Properties.Remove(property);
                    Type type = property.GetType();
                    if (list.Traits?.TryGetValue(type, out var trait) == true && trait == property)
                        list.Traits.Remove(type);
                    continue;
                }

                _properties.Add(property);
                if (property is ITriggerCallbackSync syncProperty)
                {
                    syncProperty.SyncID = (ushort)_syncList.Count;
                    _syncList.Add(syncProperty);
                }

                if (property.ID != 0)
                {
                    if (!_idToProperty.TryAdd(property.ID, property.Reference))
                        EWCLogger.Warning("Duplicate property ID detected: " + property.ID);
                }

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
        public bool TryGetReference(uint id, [MaybeNullWhen(false)] out WeaponPropertyBase property)
        {
            if (_idToProperty.TryGetValue(id, out var refProp))
            {
                property = refProp.Property;
                return true;
            }
            property = null;
            return false;
        }

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
                if (property.Reference.RefCount == 0 && property.IsProperty<WeaponClearContext>(out var clearProp))
                    clearProp.Invoke(StaticContext<WeaponClearContext>.Instance);
                else
                    s_subtreeKeepCache.Add(property);
            }

            _contextController.Clear();
            _activeTraits.Clear();
            // Add the properties back in from the cache
            foreach (var property in s_subtreePropCache)
            {
                if (!s_subtreeKeepCache.Contains(property) && property.IsProperty<WeaponSetupContext>(out var setupProp))
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
