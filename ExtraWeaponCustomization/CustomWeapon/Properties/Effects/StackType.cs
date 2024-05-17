using System;

namespace ExtraWeaponCustomization.CustomWeapon.Properties.Effects
{
    public enum StackType
    {
        Invalid = -1,
        None,
        Add,
        Multiply,
        Mult = Multiply
    }

    public static class StackTypeMethods
    {
        public static float CalculateMod(this StackType type, float mod, int count)
        {
            return type switch
            {
                StackType.Multiply or StackType.None => (float)Math.Pow(mod, count),
                StackType.Add => Math.Max(0f, 1f + mod * count),
                _ => 1f
            };
        }

        public static StackType ToStackType(this string type)
        {
            return Enum.TryParse(type, true, out StackType result) ? result : StackType.Invalid;
        }
    }
}
