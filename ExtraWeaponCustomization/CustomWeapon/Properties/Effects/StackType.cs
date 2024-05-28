using System;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public enum StackType
    {
        Invalid = -1,
        Override, None = Override,
        Add,
        Multiply,
        Mult = Multiply
    }

    public static class StackTypeMethods
    {
        public static StackType ToStackType(this string type)
        {
            return Enum.TryParse(type.Replace(" ", ""), true, out StackType result) ? result : StackType.Invalid;
        }
    }
}
