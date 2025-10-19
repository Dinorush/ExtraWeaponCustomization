using System.Collections.Generic;
using System.Linq;

namespace EWC.CustomWeapon.Properties.Effects.Debuff
{
    public abstract class DebuffHolderBase<T> where T : IDebuffGroup
    {
        private static readonly HashSet<uint> s_emptyGroup = new();
        protected readonly Dictionary<uint, T> Groups = new();
        private HashSet<uint> _currentGroup = s_emptyGroup;
        protected readonly HashSet<T> ActiveGroups = new();
        protected bool NeedRecompute = false;

        internal void Reset()
        {
            foreach (var group in Groups.Values)
                group.Reset();
            NeedRecompute = false;
            OnReset();
        }

        protected bool GroupsMatch(HashSet<uint> groups)
        {
            if (_currentGroup == groups || (_currentGroup.Count == groups.Count && _currentGroup.SetEquals(groups)))
            {
                _currentGroup = groups; // Update group pointer for faster future checks
                return true;
            }
            return false;
        }

        protected void SetActiveGroups(HashSet<uint> groups)
        {
            if (GroupsMatch(groups)) return;

            _currentGroup = groups;
            ActiveGroups.Clear();
            ActiveGroups.EnsureCapacity(groups.Count);
            foreach (var group in groups)
                if (Groups.TryGetValue(group, out var groupMod))
                    ActiveGroups.Add(groupMod);
        }

        protected T GetGroup(uint id)
        {
            if (!Groups.TryGetValue(id, out var groupMod))
                Groups.Add(id, groupMod = CreateGroup());
            return groupMod;
        }

        protected abstract T CreateGroup();
        protected abstract void OnReset();
    }
}
