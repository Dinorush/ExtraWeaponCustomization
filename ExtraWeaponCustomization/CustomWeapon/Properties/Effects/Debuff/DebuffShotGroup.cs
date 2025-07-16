using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Structs;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects.Debuff
{
    public class DebuffShotHolder : DebuffHolderBase<DebuffShotHolder.DebuffShotGroup>
    {
        private StackValue _mod = new();
        private DamageType _currentType = DamageType.Any;
        private int _needTypeStack = 0;
        private bool NeedType
        {
            get => _needTypeStack > 0;
            set => _needTypeStack += value ? 1 : -1;
        }

        public DebuffModifierBase AddModifier(float mod, StackType layer, DamageType[] damageType, uint group)
        {
            var modifier = new DebuffShotGroup.DebuffModifier(mod, layer, damageType, GetGroup(group));
            modifier.Enable();
            return modifier;
        }

        public StackValue GetMod(DamageType damageType, HashSet<uint> groups)
        {
            if (!NeedRecompute && (!NeedType || _currentType == damageType) && GroupsMatch(groups))
                return _mod;

            SetActiveGroups(groups);
            _mod.Reset();
            foreach (var group in ActiveGroups)
                _mod.Combine(group.GetMod(damageType));
            _currentType = damageType;
            NeedRecompute = false;
            return _mod;
        }

        protected override DebuffShotGroup CreateGroup() => new(this);

        protected override void OnReset()
        {
            _needTypeStack = 0;
            _currentType = DamageType.Any;
        }

        public class DebuffShotGroup : IDebuffGroup
        {
            private readonly HashSet<DebuffModifier>[] _layers = new HashSet<DebuffModifier>[StackTypeConst.Count];
            private readonly DebuffShotHolder _holder;

            private StackValue _mod = new();

            private DamageType _currentType = DamageType.Any;
            private int _needTypeStack = 0;
            private bool NeedType
            {
                get => _needTypeStack > 0;
                set
                {
                    if (value)
                    {
                        if (_needTypeStack++ == 0)
                            _holder.NeedType = true;
                    }
                    else
                    {
                        if (--_needTypeStack == 0)
                            _holder.NeedType = false;
                    }
                }
            }
            private uint _recomputeMask = 0;

            public DebuffShotGroup(DebuffShotHolder holder) => _holder = holder;

            public StackValue GetMod(DamageType damageType)
            {
                bool correctType = !NeedType || _currentType == damageType;
                if (_recomputeMask == 0 && correctType)
                    return _mod;

                _currentType = damageType;
                if (!correctType)
                {
                    _recomputeMask = 0;
                    for (int i = 0; i < StackTypeConst.Count; i++)
                        Recompute((StackType)i, damageType);
                }
                else
                {
                    for (uint i = 0; _recomputeMask != 0; i++)
                    {
                        if ((_recomputeMask & 1) != 0)
                            Recompute((StackType)i, damageType);
                        _recomputeMask >>= 1;
                    }
                }

                return _mod;
            }

            public void Reset()
            {
                foreach (var layer in _layers)
                {
                    if (layer == null) continue;

                    foreach (var modifier in layer)
                        modifier.Active = false;
                    layer.Clear();
                }

                _mod.Reset();
                _needTypeStack = 0;
                _currentType = DamageType.Any;
                _recomputeMask = 0;
            }

            private HashSet<DebuffModifier> GetLayer(StackType layer) => _layers[(int)layer] ?? (_layers[(int)layer] = new());

            private void Refresh(StackType layer, DamageType damageType)
            {
                if ((NeedType && !_currentType.HasFlag(damageType)) || _layers[(int)layer] == null) return;

                _recomputeMask |= 1u << (int)layer;
                _holder.NeedRecompute = true;
            }

            private void Recompute(StackType layer, DamageType damageType)
            {
                if (_layers[(int)layer] == null) return;

                _mod.Reset(layer);
                foreach (var modifier in GetLayer(layer))
                    if (damageType.HasFlagIn(modifier.DamageType))
                        _mod.Add(modifier.Mod, layer);
            }

            public class DebuffModifier : DebuffModifierBase
            {
                public DamageType[] DamageType { get; }

                private readonly StackType _layer;
                private readonly DebuffShotGroup _group;
                private readonly bool _hasType;

                public DebuffModifier(float mod, StackType layer, DamageType[] damageType, DebuffShotGroup group) : base(mod)
                {
                    DamageType = damageType;
                    _group = group;
                    _layer = layer;
                    _hasType = damageType.Length > 1 || (damageType.Length == 1 && damageType[0] != Enums.DamageType.Any);
                }

                protected override void RefreshGroup()
                {
                    foreach (var type in DamageType)
                        _group.Refresh(_layer, type);
                }

                protected override void AddToGroup()
                {
                    _group.GetLayer(_layer).Add(this);
                    if (_hasType)
                        _group.NeedType = true;
                }

                protected override void RemoveFromGroup()
                {
                    _group.GetLayer(_layer).Remove(this);
                    if (_hasType)
                        _group.NeedType = false;
                }
            }
        }
    }
}
