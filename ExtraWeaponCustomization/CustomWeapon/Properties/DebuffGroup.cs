using EWC.CustomWeapon.Properties.Effects.Debuff;
using System.Collections.Generic;

namespace EWC.CustomWeapon.Properties
{
    public sealed class DebuffGroup
    {
        public static readonly DebuffGroup Default = new();
        public static HashSet<uint> DefaultGroupList => DebuffManager.DefaultGroupSet;

        public HashSet<uint> IDs { get; set; } = DefaultGroupList;
    }
}
