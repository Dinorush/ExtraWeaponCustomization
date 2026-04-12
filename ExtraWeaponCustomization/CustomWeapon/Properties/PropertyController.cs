using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Shared.Triggers;
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
        private bool _hasTempProperties = false;
        private readonly ContextController _contextController;
        private readonly List<WeaponPropertyBase> _properties = new();
        private readonly LinkedList<PropertyNode> _overrideStack = new();
        private readonly Dictionary<Type, Trait> _activeTraits = new();
        private readonly Dictionary<uint, WeaponPropertyBase> _idToProperty = new();
        private readonly List<ITriggerCallbackSync> _syncList = new(1);

        // Used when moving to an override root
        private static readonly List<WeaponPropertyBase> s_subtreePropCache = new();
        private static readonly HashSet<WeaponPropertyBase> s_subtreeKeepCache = new();

        public PropertyController(Enums.OwnerType ownerType, Enums.WeaponType weaponType)
        {
            _contextController = new(ownerType, weaponType);
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
            ReplaceReferences(_root.List);
            foreach (var property in _properties)
                property.OnPropertiesSetup();
            SetActive(_root, true);
        }

        private PropertyNode CreateTree(CustomWeaponComponent cwc, PropertyList list, PropertyNode? parent = null, IPropertyHolder? holder = null)
        {
            PropertyNode curNode = new(list, parent, holder);
            list.SetCWC(cwc);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var property = list[i];
                if (property is ITriggerCallbackSync syncProperty)
                {
                    syncProperty.SyncID = (ushort)_syncList.Count;
                    _syncList.Add(syncProperty);
                }

                if (!property.ValidProperty())
                {
                    list.Values.RemoveAt(i);
                    continue;
                }

                if (property.ID != 0)
                {
                    if (!_idToProperty.TryAdd(property.ID, property))
                        EWCLogger.Warning("Duplicate property ID detected: " + property.ID);
                }

                if (property is not ReferenceProperty)
                    _properties.Add(property);

                if (property is IPropertyHolder propHolder && !propHolder.Properties.Empty)
                {
                    _hasTempProperties = _hasTempProperties || property is TempProperties;
                    CreateTree(cwc, propHolder.Properties, curNode, propHolder);
                }
            }
            return curNode;
        }

        private void ReplaceReferences(PropertyList list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] is IPropertyHolder propHolder)
                {
                    ReplaceReferences(propHolder.Properties);
                    continue;
                }
                if (list[i] is not ReferenceProperty referenceProperty) continue;

                if (!_idToProperty.TryGetValue(referenceProperty.ReferenceID, out var property))
                    list.Values.RemoveAt(i);
                else
                    list.Values[i] = property;
            }
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
        public bool HasTempProperties() => _hasTempProperties;
        public bool HasTrait<T>() where T : Trait => _activeTraits.ContainsKey(typeof(T));
        public T? GetTrait<T>() where T : Trait => _activeTraits.TryGetValueAs<Type, Trait, T>(typeof(T), out T? trait) ? trait : null;
        public bool TryGetTrait<T>([MaybeNullWhen(false)] out T trait) where T : Trait => _activeTraits.TryGetValueAs(typeof(T), out trait);
        public bool TryGetProperty(uint id, [MaybeNullWhen(false)] out WeaponPropertyBase property) => _idToProperty.TryGetValue(id, out property);

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
            if (node.Active == active) return;
            node.Active = active;

            if (active)
                Activate(node);
            else
                Deactivate(node);
        }

        private void Activate(PropertyNode node)
        {
            if (node.Override)
            {
                if (node.Enabled)
                    UpdateRoot(node);
                else
                    _overrideStack.AddBefore(_overrideStack.Last!, node);
                return;
            }

            if (!node.Enabled) return;

            foreach (var property in node.List)
            {
                if (property.RefCount++ != 0) continue;

                if (property is Trait trait && !_activeTraits.TryAdd(property.GetType(), trait)) continue;

                if (property.IsProperty<WeaponSetupContext>(out var setup))
                    setup.Invoke(StaticContext<WeaponSetupContext>.Instance);

                _contextController.Register(property);
            }
        }

        private void Deactivate(PropertyNode node)
        {
            if (node.Override)
            {
                _overrideStack.Remove(node);
                if (node.Enabled)
                    UpdateRoot();
                return;
            }

            if (!node.Enabled) return;

            foreach (var property in node.List)
            {
                if (--property.RefCount != 0) continue;

                Type type = property.GetType();
                if (property is Trait trait)
                { 
                    if (!_activeTraits.TryGetValue(type, out var active) || active != trait)
                        continue;
                    else
                        _activeTraits.Remove(type);
                }

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
                property.RefCount = 0;

            node ??= _root;

            void SetSubtree(PropertyNode curr, bool enabled = false)
            {
                if (curr == node)
                {
                    // Keep the Property Holders that had the override in the subtree
                    if (curr.Owner != null && curr.Owner.RefCount++ == 0)
                        s_subtreePropCache.Add((WeaponPropertyBase)curr.Owner);
                    enabled = true;
                }
                // If subtree already added by reference property, ignore it
                else if (curr.Owner != null && curr.Owner.RefCount > 0)
                    return;

                curr.Enabled = enabled;

                // Add all properties from active nodes
                if (enabled && curr.Active)
                {
                    foreach (var property in curr.List)
                    {
                        if (property.RefCount == 0) // Only add a single property once
                        {
                            s_subtreePropCache.Add(property);

                            if (property is IPropertyHolder propHolder && propHolder.Node != null)
                                SetSubtree(propHolder.Node, enabled);
                        }

                        property.RefCount++;
                    }
                }

                foreach (var child in curr.Children)
                    SetSubtree(child, enabled);
            }

            SetSubtree(_root);

            // Clear any properties that were not preserved
            foreach (var property in _contextController.Properties)
            {
                if (property.RefCount != 0)
                    s_subtreeKeepCache.Add(property);
                else if (property.IsProperty<WeaponClearContext>(out var clearProp))
                    clearProp.Invoke(StaticContext<WeaponClearContext>.Instance);
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
