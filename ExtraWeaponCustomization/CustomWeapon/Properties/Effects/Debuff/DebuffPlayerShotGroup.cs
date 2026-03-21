using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Structs;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects.Debuff
{
    public class DebuffPlayerShotHolder : DebuffHolderBase<DebuffPlayerShotHolder.DebuffPlayerShotGroup>
    {
        private StackValue _mod = new();
        private readonly DebuffPlayerShotGroup _group;

        public DebuffPlayerShotHolder()
        {
            _group = new(this);
        }

        public DebuffModifierBase AddModifier(float mod, StackType layer, PlayerDamageType[] damageType)
        {
            var modifier = new DebuffPlayerShotGroup.DebuffModifier(mod, layer, damageType, _group);
            modifier.Enable();
            return modifier;
        }

        public DebuffModifierBase AddImmuneModifier(PlayerDamageType[] damageType)
        {
            var modifier = new DebuffPlayerShotGroup.ImmuneModifier(damageType, _group);
            modifier.Enable();
            return modifier;
        }

        public bool IsImmune(PlayerDamageType damageType) => _group.IsImmune(damageType);
        public StackValue GetMod(PlayerDamageType damageType) => _group.GetMod(damageType);

        protected override DebuffPlayerShotGroup CreateGroup() => _group;

        protected override void OnReset()
        {
            _mod.Reset();
        }

        public class DebuffPlayerShotGroup : IDebuffGroup
        {
            private readonly HashSet<DebuffModifier>[] _layers = new HashSet<DebuffModifier>[StackTypeConst.Count];
            private readonly HashSet<ImmuneModifier> _immunities = new();
            private readonly DebuffPlayerShotHolder _holder;

            private StackValue _mod = new();
            private bool _immune = false;

            private PlayerDamageType _currentType = PlayerDamageType.Any;
            private int _needTypeStack = 0;
            private bool NeedType
            {
                get => _needTypeStack > 0;
                set => _needTypeStack += value ? 1 : -1;
            }
            private uint _recomputeMask = 0;
            private bool _recomputeImmune = false;

            public DebuffPlayerShotGroup(DebuffPlayerShotHolder holder) => _holder = holder;

            public bool IsImmune(PlayerDamageType damageType)
            {
                bool correctType = !NeedType || _currentType == damageType;
                if (_recomputeImmune || !correctType)
                    RecomputeImmunity(damageType);
                return _immune;
            }

            public StackValue GetMod(PlayerDamageType damageType)
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
                _currentType = PlayerDamageType.Any;
                _recomputeMask = 0;
            }

            private HashSet<DebuffModifier> GetLayer(StackType layer) => _layers[(int)layer] ?? (_layers[(int)layer] = new());

            private void Refresh(StackType layer, PlayerDamageType damageType)
            {
                if ((NeedType && !_currentType.HasFlag(damageType)) || _layers[(int)layer] == null) return;

                _recomputeMask |= 1u << (int)layer;
            }

            private void RefreshImmune(PlayerDamageType damageType)
            {
                if (NeedType && !_currentType.HasFlag(damageType)) return;

                _recomputeImmune = true;
            }

            private void Recompute(StackType layer, PlayerDamageType damageType)
            {
                if (_layers[(int)layer] == null) return;

                _mod.Reset(layer);
                foreach (var modifier in GetLayer(layer))
                    if (damageType.HasFlagIn(modifier.DamageType))
                        _mod.Add(modifier.Mod, layer);
            }

            private void RecomputeImmunity(PlayerDamageType damageType)
            {
                foreach (var modifier in _immunities)
                {
                    if (damageType.HasFlagIn(modifier.DamageType))
                    {
                        _immune = true;
                        return;
                    }
                }
            }

            public class DebuffModifier : DebuffModifierBase
            {
                public PlayerDamageType[] DamageType { get; }
                private readonly StackType _layer;
                private readonly DebuffPlayerShotGroup _group;
                private readonly bool _hasType;

                public DebuffModifier(float mod, StackType layer, PlayerDamageType[] damageType, DebuffPlayerShotGroup group) : base(mod)
                {
                    DamageType = damageType;
                    _group = group;
                    _layer = layer;
                    _hasType = damageType.Length > 1 || (damageType.Length == 1 && damageType[0] != PlayerDamageType.Any);
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

            public class ImmuneModifier : DebuffModifierBase
            {
                public PlayerDamageType[] DamageType { get; }
                private readonly DebuffPlayerShotGroup _group;
                private readonly bool _hasType;

                public ImmuneModifier(PlayerDamageType[] damageType, DebuffPlayerShotGroup group) : base(1f)
                {
                    DamageType = damageType;
                    _group = group;
                    _hasType = damageType.Length > 1 || (damageType.Length == 1 && damageType[0] != PlayerDamageType.Any);
                }

                protected override void RefreshGroup()
                {
                    foreach (var type in DamageType)
                        _group.RefreshImmune(type);
                }

                protected override void AddToGroup()
                {
                    _group._immunities.Add(this);
                    if (_hasType)
                        _group.NeedType = true;
                }

                protected override void RemoveFromGroup()
                {
                    _group._immunities.Remove(this);
                    if (_hasType)
                        _group.NeedType = false;
                }
            }
        }
    }
}
