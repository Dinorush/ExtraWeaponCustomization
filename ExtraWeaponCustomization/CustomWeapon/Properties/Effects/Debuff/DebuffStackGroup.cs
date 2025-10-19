using EWC.CustomWeapon.Enums;
using EWC.CustomWeapon.Structs;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects.Debuff
{
    public class DebuffStackHolder : DebuffHolderBase<DebuffStackHolder.DebuffStackGroup>
    {
        private StackValue _mod = new();

        public DebuffModifierBase AddModifier(float mod, StackType layer, uint group)
        {
            var modifier = new DebuffStackGroup.DebuffModifier(mod, layer, GetGroup(group));
            modifier.Enable();
            return modifier;
        }

        public StackValue GetMod(HashSet<uint> groups)
        {
            if (!NeedRecompute && GroupsMatch(groups))
                return _mod;

            SetActiveGroups(groups);
            _mod.Reset();
            foreach (var group in ActiveGroups)
                _mod.Combine(group.GetMod());
            NeedRecompute = false;
            return _mod;
        }

        protected override DebuffStackGroup CreateGroup() => new(this);

        protected override void OnReset()
        {
            _mod.Reset();
        }

        public class DebuffStackGroup : IDebuffGroup
        {
            private readonly HashSet<DebuffModifier>[] _layers = new HashSet<DebuffModifier>[StackTypeConst.Count];
            private readonly DebuffStackHolder _holder;

            private StackValue _mod = new();
            private uint _recomputeMask = 0;

            public DebuffStackGroup(DebuffStackHolder holder) => _holder = holder;

            public StackValue GetMod()
            {
                if (_recomputeMask == 0)
                    return _mod;

                for (uint i = 0; _recomputeMask != 0; i++)
                {
                    if ((_recomputeMask & 1) != 0)
                        Recompute((StackType)i);
                    _recomputeMask >>= 1;
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
                _recomputeMask = 0;
            }

            private HashSet<DebuffModifier> GetLayer(StackType layer) => _layers[(int)layer] ?? (_layers[(int)layer] = new());

            private void Refresh(StackType layer)
            {
                if (_layers[(int)layer] == null) return;

                _recomputeMask |= 1u << (int)layer;
                _holder.NeedRecompute = true;
            }

            private void Recompute(StackType layer)
            {
                if (_layers[(int)layer] == null) return;

                _mod.Reset(layer);
                foreach (var modifier in GetLayer(layer))
                    _mod.Add(modifier.Mod, layer);
            }

            public class DebuffModifier : DebuffModifierBase
            {
                private readonly StackType _layer;
                private readonly DebuffStackGroup _group;

                public DebuffModifier(float mod, StackType layer, DebuffStackGroup group) : base(mod)
                {
                    _group = group;
                    _layer = layer;
                }

                protected override void RefreshGroup()
                {
                    _group.Refresh(_layer);
                }

                protected override void AddToGroup()
                {
                    _group.GetLayer(_layer).Add(this);
                }

                protected override void RemoveFromGroup()
                {
                    _group.GetLayer(_layer).Remove(this);
                }
            }
        }
    }
}
