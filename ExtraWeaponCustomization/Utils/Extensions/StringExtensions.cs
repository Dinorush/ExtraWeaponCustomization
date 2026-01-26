using System;

namespace EWC.Utils.Extensions
{
    internal static class StringExtensions
    {
        public static T ToEnum<T>(this string? value, T defaultValue) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return defaultValue;

            return Enum.TryParse(value.Replace(" ", null), true, out T result) ? result : defaultValue;
        }

        public static bool ContainsAny(this string input, params string[] args)
        {
            foreach (string value in args)
            {
                if (input.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
