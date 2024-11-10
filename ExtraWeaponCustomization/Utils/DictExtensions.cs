using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace EWC.Utils
{
    internal static class DictExtensions
    {
        public static bool TryGetValueAs<Key, Value, ValueAs>(this IDictionary<Key, Value> dict, Key key, [MaybeNullWhen(false)] out ValueAs valueAs)
            where Key : notnull
            where ValueAs : Value
        {
            if (dict.TryGetValue(key, out Value? value))
            {
                valueAs = (ValueAs)value!;
                return true;
            }
            valueAs = default;
            return false;
        }
    }
}
