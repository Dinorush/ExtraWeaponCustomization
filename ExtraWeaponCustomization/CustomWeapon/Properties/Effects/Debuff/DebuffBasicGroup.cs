using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties.Effects.Debuff
{
    public class DebuffBasicHolder : DebuffHolderBase<DebuffBasicHolder.DebuffBasicGroup>
    {
        private float _mod = 1f;

        public DebuffModifierBase AddModifier(float mod, uint group)
        {
            var modifier = new DebuffBasicGroup.DebuffModifier(mod, GetGroup(group));
            modifier.Enable();
            return modifier;
        }

        public float GetMod(HashSet<uint> groups)
        {
            if (!NeedRecompute && GroupsMatch(groups))
                return _mod;

            SetActiveGroups(groups);
            _mod = 1f;
            foreach (var group in ActiveGroups)
                _mod *= group.GetMod();
            NeedRecompute = false;
            return _mod;
        }

        protected override DebuffBasicGroup CreateGroup() => new(this);

        protected override void OnReset()
        {
            _mod = 1f;
        }

        public class DebuffBasicGroup : IDebuffGroup
        {
            private readonly HashSet<DebuffModifier> _modifiers = new();
            private readonly DebuffBasicHolder _holder;

            private float _mod = 1f;
            private bool _needRecompute = false;

            public DebuffBasicGroup(DebuffBasicHolder holder) => _holder = holder;

            public float GetMod()
            {
                if (!_needRecompute)
                    return _mod;

                _mod = 1f;
                foreach (var modifier in _modifiers)
                    _mod *= modifier.Mod;
                _needRecompute = false;
                return _mod;
            }

            void IDebuffGroup.Reset()
            {
                foreach (var modifier in _modifiers)
                    modifier.Active = false;
                _modifiers.Clear();

                _mod = 1f;
            }

            private void Refresh()
            {
                _needRecompute = true;
                _holder.NeedRecompute = true;
            }

            public class DebuffModifier : DebuffModifierBase
            {
                private readonly DebuffBasicGroup _group;

                public DebuffModifier(float mod, DebuffBasicGroup group) : base(mod)
                {
                    _group = group;
                }

                protected override void AddToGroup()
                {
                    _group._modifiers.Add(this);
                }

                protected override void RemoveFromGroup()
                {
                    _group._modifiers.Remove(this);
                }

                protected override void RefreshGroup()
                {
                    _group.Refresh();
                }
            }
        }
    }
}
