using EWC.CustomWeapon.WeaponContext;
using System.Collections.Generic;
using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.WeaponContext.Contexts;
using System;
using EWC.CustomWeapon.Properties.Traits;
using EWC.CustomWeapon.Properties.Effects.Triggers;
using EWC.Utils.Log;

namespace EWC.CustomWeapon.Properties
{
    internal sealed class PropertyNode
    {
        public readonly PropertyList List;
        public readonly List<PropertyNode> Children;
        public bool Active { get; set; } = false;
        public bool Enabled { get; set; } = true;

        public PropertyNode(PropertyList list, PropertyNode? parent)
        {
            List = list;
            Children = new List<PropertyNode>();
            parent?.Children.Add(this);
        }
    }

    internal sealed class PropertyController
    {
        private PropertyNode? _root;
        private readonly ContextController _contextController;
        private readonly LinkedList<PropertyNode> _overrideStack = new();
        private readonly Dictionary<Type, Trait> _activeTraits = new();
        private readonly List<ITriggerCallbackSync> _syncList = new(1);

        public PropertyController(bool isGun)
        {
            _contextController = new(isGun);
        }

        public TContext Invoke<TContext>(TContext context) where TContext : IWeaponContext => _contextController.Invoke(context);

        public void Init(CustomWeaponComponent cwc, PropertyList? baseList)
        {
            if (baseList == null) return;

            _root = CreateTree(cwc, baseList);
            RemoveInvalidProperties(_root, cwc.IsGun);
            Activate(_root);
            RegisterSyncProperties(_root);
        }

        private PropertyNode CreateTree(CustomWeaponComponent cwc, PropertyList list, PropertyNode? parent = null)
        {
            PropertyNode curNode = new(list, parent);
            foreach (WeaponPropertyBase property in list.Properties)
            {
                property.CWC = cwc;
                if (property is TempProperties tempProperties)
                {
                    if (tempProperties.Properties == null) continue;

                    tempProperties.Node = CreateTree(cwc, tempProperties.Properties, curNode);
                }
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
                    node.List.Properties.Remove(property);
                    Type type = property.GetType();
                    if (node.List.Traits?.TryGetValue(type, out var trait) == true && trait == property)
                        node.List.Traits.Remove(type);
                }
            }

            foreach (var child in node.Children)
                RemoveInvalidProperties(child, isGun);
        }

        internal void ChangeToSyncContexts()
        {
            _contextController.ChangeToSyncContexts();
        }

        private void RegisterSyncProperties(PropertyNode node)
        {
            foreach (var property in node.List.Properties)
            {
                if (property is ITriggerCallbackSync syncProperty)
                {
                    syncProperty.SyncID = (ushort) _syncList.Count;
                    _syncList.Add(syncProperty);
                }
            }

            foreach (var child in node.Children)
                RegisterSyncProperties(child);
        }

        public void Clear()
        {
            _contextController.Clear();
            _overrideStack.Clear();
            _activeTraits.Clear();
            _syncList.Clear();
        }

        public ContextController GetContextController() => _contextController;
        public bool HasTempProperties() => _root!.Children.Count > 0;
        public bool HasTrait(Type type) => _activeTraits.ContainsKey(type);
        public Trait GetTrait(Type type) => _activeTraits[type];
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
                {
                    // UpdateRoot will register/add traits, so just need to invoke setups
                    UpdateRoot(node);

                    if (node.List.Traits != null)
                    {
                        foreach (Trait trait in node.List.Traits.Values)
                        {
                            if (trait is IWeaponProperty<WeaponSetupContext> setup)
                                setup.Invoke(StaticContext<WeaponSetupContext>.Instance);
                        }
                    }
                }
                else
                    _overrideStack.AddBefore(_overrideStack.Last!, node);
            }

            if (!node.Enabled) return;

            foreach (var property in node.List.Properties)
                _contextController.Register(property);
            
            if (node.List.Traits != null)
            {
                foreach ((Type type, Trait trait) in node.List.Traits)
                {
                    if (_activeTraits.TryAdd(type, trait))
                    {
                        if (trait is IWeaponProperty<WeaponSetupContext> setup)
                            setup.Invoke(StaticContext<WeaponSetupContext>.Instance);
                    }
                }
            }
        }

        private void Deactivate(PropertyNode node)
        {
            if (!node.Active) return;
            node.Active = false;

            if (node.List.Override)
                _overrideStack.Remove(node);

            if (!node.Enabled) return;

            foreach (var property in node.List.Properties)
                _contextController.Unregister(property);

            if (node.List.Traits != null)
            {
                foreach ((Type type, Trait trait) in node.List.Traits)
                {
                    if (_activeTraits[type] == trait && _activeTraits.Remove(type))
                    {
                        if (trait is IWeaponProperty<WeaponClearContext> setup)
                            setup.Invoke(StaticContext<WeaponClearContext>.Instance);
                    }
                }
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

            if (node != null)
            {
                PropagateDisable(_root);
                _activeTraits.Clear();
                PropagateEnable(node);
                if (node.List.Owner != null)
                    _contextController.Register(node.List.Owner);
            }
            else
            {
                _activeTraits.Clear();
                PropagateEnable(_root);
            }
        }

        private void PropagateDisable(PropertyNode? node)
        {
            if (node == null || !node.Enabled || node == _overrideStack.Last!.Value) return;
            node.Enabled = false;

            if (node.Active)
            {
                if (node.List.Traits != null)
                {
                    foreach ((Type type, Trait trait) in node.List.Traits)
                    {
                        if (_activeTraits[type] == trait)
                        {
                            if (trait is IWeaponProperty<WeaponClearContext> setup)
                                setup.Invoke(StaticContext<WeaponClearContext>.Instance);
                        }
                    }
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
                    foreach (var property in node.List.Properties)
                        _contextController.Register(property);

                    if (node.List.Traits != null)
                        foreach ((Type type, Trait trait) in node.List.Traits)
                            _activeTraits.TryAdd(type, trait);
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
