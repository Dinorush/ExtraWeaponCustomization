using System;
using System.Linq;

namespace EWC.CustomWeapon.Enums
{
    public enum StackType
    {
        Invalid = -1,
        Override, None = Override,
        Add,
        Multiply, Mult = Multiply,
        Max,
        Min
    }

    public static class StackTypeConst
    {
        public static readonly int Count = (int)Enum.GetValues<StackType>().Max() + 1;
    }
}
