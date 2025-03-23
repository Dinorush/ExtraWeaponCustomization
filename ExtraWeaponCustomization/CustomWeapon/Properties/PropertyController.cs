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
        private PropertyNode? _root;
        private readonly ContextController _contextController;
        private readonly LinkedList<PropertyNode> _overrideStack = new();
        private readonly Dictionary<Type, Trait> _activeTraits = new();
        private readonly Dictionary<uint, PropertyRef> _idToProperty = new();
        private static readonly HashSet<WeaponPropertyBase> s_ignoreRefs = new();
        private readonly List<ITriggerCallbackSync> _syncList = new(1);

        public PropertyController(bool isGun, bool isLocal)
        {
            _contextController = new(isGun, isLocal);
        }

        public TContext Invoke<TContext>(TContext context) where TContext : IWeaponContext => _contextController.Invoke(context);

        public void Init(CustomWeaponComponent cwc, PropertyList? baseList)
        {
            if (baseList == null) return;

            _root = CreateTree(cwc, baseList);
            RemoveInvalidProperties(_root, cwc.IsGun);
            RegisterPropertyIDs();
            Activate(_root);
            RegisterSyncProperties(_root);
            Invoke(StaticContext<WeaponInitContext>.Instance);
        }

        private PropertyNode CreateTree(CustomWeaponComponent cwc, PropertyList list, PropertyNode? parent = null)
        {
            PropertyNode curNode = new(list, parent);
            list.SetCWC(cwc);
            foreach (WeaponPropertyBase property in list.Properties)
            {
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

        private void RegisterSyncProperties(PropertyNode node)
        {
            foreach (var property in node.List.Properties)
            {
                if (property is ITriggerCallbackSync syncProperty)
                {
                    syncProperty.SyncID = (ushort)_syncList.Count;
                    _syncList.Add(syncProperty);
                }
            }

            foreach (var child in node.Children)
                RegisterSyncProperties(child);
        }

        private void RegisterPropertyIDs()
        {
            if (_root == null) return;

            CachePropertyIDs(_root);
            SetReferenceProperties(_root);
        }

        private void CachePropertyIDs(PropertyNode node)
        {
            foreach (var property in node.List.Properties)
            {
                if (property.ID != 0)
                {
                    if (!_idToProperty.TryAdd(property.ID, new PropertyRef(property)))
                        EWCLogger.Warning("Duplicate property ID detected: " + property.ID);
                }
            }

            foreach (var child in node.Children)
                CachePropertyIDs(child);
        }

        private void SetReferenceProperties(PropertyNode node)
        {
            foreach (var refHolder in node.List.ReferenceHolders.OrEmptyIfNull())
            {
                foreach (var property in refHolder.Properties.ReferenceProperties.OrEmptyIfNull())
                {
                    if (_idToProperty.TryGetValue(property.ReferenceID, out var refProperty))
                    {
                        property.Reference = refProperty;
                        refHolder.OnReferenceSet(refProperty.property);
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
            _contextController.Clear();
            _overrideStack.Clear();
            _activeTraits.Clear();
            _idToProperty.Clear();
            _syncList.Clear();
        }

        public ContextController GetContextController() => _contextController;
        public bool HasTempProperties() => _root!.Children.Count > 0;
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
            if (node.List.Override)
            {
                PropertyNode newRoot = active ? node : (_overrideStack.Count > 1 ? _overrideStack.Last!.Previous!.Value : _root!);
                CacheIgnoreRefs(newRoot, active);
            }

            if (active)
                Activate(node);
            else
                Deactivate(node);

            if (node.List.Override)
                s_ignoreRefs.Clear();
        }

        private void Activate(PropertyNode node)
        {
            if (node.Active) return;
            node.Active = true;

            if (node.List.Override)
            {
                if (node.Enabled)
                {
                    // UpdateRoot will register/add traits, so just need to invoke setups
                    UpdateRoot(node);

                    foreach (var property in node.List.SetupCallbacks.OrEmptyIfNull())
                    {
                        if (s_ignoreRefs.Contains((WeaponPropertyBase) property)) continue;

                        property.Invoke(StaticContext<WeaponSetupContext>.Instance);
                    }
                }
                else
                    _overrideStack.AddBefore(_overrideStack.Last!, node);
            }

            if (!node.Enabled) return;

            foreach (var curr in node.List.Properties)
            {
                var property = curr;
                // Swap ReferenceProperties with their target, or skip it if it's referenced elsewhere
                if (property is ReferenceProperty refProp)
                {
                    if (refProp.Reference == null || refProp.Reference.refCount++ != 0) continue;
                    property = refProp.Reference.property;
                }
                else if (property.ID != 0 && _idToProperty[property.ID].refCount++ != 0) continue;

                if (s_ignoreRefs.Contains(property)) continue; // Ignore properties that will remain active

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
                _overrideStack.Remove(node);

            if (!node.Enabled) return;

            foreach (var curr in node.List.Properties)
            {
                var property = curr;
                // Swap ReferenceProperties with their target, or skip it if it's referenced elsewhere
                if (property is ReferenceProperty refProp)
                {
                    if (refProp.Reference == null || --refProp.Reference.refCount != 0) continue;
                    property = refProp.Reference.property;
                }
                else if (property.ID != 0 && --_idToProperty[property.ID].refCount != 0) continue;

                if (s_ignoreRefs.Contains(property)) continue; // Ignore properties that will remain active

                Type type = property.GetType();
                if (property is Trait trait && (!_activeTraits.TryGetValue(type, out var active) || active != trait))
                    continue;
                else
                    _activeTraits.Remove(type);

                if (property.IsProperty<WeaponClearContext>(out var clear))
                    clear.Invoke(StaticContext<WeaponClearContext>.Instance);

                _contextController.Unregister(property);
            }

            if (node.List.Override)
                UpdateRoot();
        }

        private void UpdateRoot(PropertyNode? node = null)
        {
            if (node != null)
                _overrideStack.AddLast(node);
            else
                node = _overrideStack.Last?.Value;

            _contextController.Clear();
            // Register properties that will remain active (they will not be added by other functions)
            foreach (var property in s_ignoreRefs)
                _contextController.Register(property);

            if (node != null)
            {
                PropagateDisable(_root);
                _activeTraits.Clear();
                foreach (var property in s_ignoreRefs)
                    if (property is Trait trait)
                        _activeTraits.TryAdd(property.GetType(), trait);
                if (node.List.Owner != null)
                    _contextController.Register(node.List.Owner);
                PropagateEnable(node);
            }
            else
            {
                _activeTraits.Clear();
                PropagateEnable(_root);
            }
        }

        private void CacheIgnoreRefs(PropertyNode? node, bool allowFirst = true)
        {
            if (node == null || (!node.Active && !allowFirst)) return;

            foreach (var refProp in node.List.ReferenceProperties.OrEmptyIfNull())
                if (refProp.Reference != null)
                    s_ignoreRefs.Add(refProp.Reference.property);

            foreach (var child in node.Children)
                CacheIgnoreRefs(child, false);
        }

        private void PropagateDisable(PropertyNode? node)
        {
            if (node == null || !node.Enabled || node == _overrideStack.Last!.Value) return;
            node.Enabled = false;

            if (node.Active && node.List.ClearCallbacks != null)
            {
                foreach (var property in node.List.ClearCallbacks)
                {
                    if (s_ignoreRefs.Contains((WeaponPropertyBase)property)) continue; // Ignore properties that will remain active

                    if (property is Trait trait && (!_activeTraits.TryGetValue(property.GetType(), out var active) || trait != active)) continue;

                    property.Invoke(StaticContext<WeaponClearContext>.Instance);
                }
            }

            foreach (var child in node.Children)
                PropagateDisable(child);
        }

        private void PropagateEnable(PropertyNode? node)
        {
            if (node == null) return;

            if (node.Enabled)
            {
                // Re-register properties since data was cleared, but no need to call setup context
                if (node.Active)
                {
                    foreach (var curr in node.List.Properties)
                    {
                        var property = curr;
                        if (property is ReferenceProperty refProp)
                        {
                            if (refProp.Reference == null) continue;
                            property = refProp.Reference.property;
                        }
                        if (s_ignoreRefs.Contains(property)) continue; // Ignore properties that will remain active

                        if (property is Trait trait && !_activeTraits.TryAdd(property.GetType(), trait)) continue;

                        _contextController.Register(property);
                    }
                }
                return;
            }

            node.Enabled = true;

            if (node.Active)
            {
                node.Active = false;
                Activate(node);
            }

            foreach (var child in node.Children)
                PropagateEnable(child);
        }
    }
}
