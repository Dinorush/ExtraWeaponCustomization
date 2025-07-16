using System;
using System.Linq;

namespace EWC.CustomWeapon.Enums
{
    public enum StatType
    {
        Damage,
        Precision, Prec = Precision,
        Stagger
    }

    public static class StatTypeConst
    {
        public static readonly int Count = (int)Enum.GetValues<StatType>().Max() + 1;
    }
}
