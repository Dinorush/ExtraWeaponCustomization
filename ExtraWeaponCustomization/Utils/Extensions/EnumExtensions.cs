using System;
using System.Runtime.CompilerServices;

namespace EWC.Utils.Extensions
{
    internal static class EnumExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasAnyFlag<T>(this T value, T flag) where T : Enum
        {
            int valNum = Convert.ToInt32(value);
            int flagNum = Convert.ToInt32(flag);
            return flagNum == 0 || (valNum & flagNum) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasFlagIn(this Enum type, Enum[] flagSet)
        {
            foreach (var flag in flagSet)
                if (type.HasFlag(flag))
                    return true;
            return false;
        }
    }
}
