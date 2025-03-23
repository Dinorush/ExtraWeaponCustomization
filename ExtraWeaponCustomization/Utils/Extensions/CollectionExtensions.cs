using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace EWC.Utils.Extensions
{
    internal static class CollectionExtensions
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

        public static IReadOnlyDictionary<Key, Value> OrEmptyIfNull<Key, Value>(this IReadOnlyDictionary<Key, Value>? dict)
            where Key : notnull
        {
            return dict ?? ImmutableDictionary<Key, Value>.Empty;
        }

        public static IReadOnlyList<T> OrEmptyIfNull<T>(this IReadOnlyList<T>? list)
        {
            return list ?? ImmutableList<T>.Empty;
        }
    }
}
