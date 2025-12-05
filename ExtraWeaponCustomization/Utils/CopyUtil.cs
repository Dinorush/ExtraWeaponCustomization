using EWC.CustomWeapon.Properties;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EWC.Utils
{
    public static class CopyUtil
    {
        public static T Clone<T>(T obj, params object?[]? args) where T : notnull => CopyUtil<T>.Clone(obj, args);
    }

    public static class CopyUtil<T> where T : notnull
    {
        private static readonly Dictionary<Type, List<PropertyInfo>> _classProperties = new();

        public static T Clone(T obj, params object?[]? args)
        {
            Type type = obj.GetType();
            if (!_classProperties.ContainsKey(type))
            {
                List<PropertyInfo> properties = new();
                _classProperties.Add(type, properties);

                for (Type currentType = type; currentType.BaseType != null; currentType = currentType.BaseType)
                {
                    foreach (var prop in currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                    {
                        MethodInfo? mget = prop.GetGetMethod(false);
                        MethodInfo? mset = prop.GetSetMethod(true);

                        // Only want properties with public get and private set
                        if (mget == null || mset == null || mset.IsPublic || !prop.CanWrite) continue;

                        properties.Add(prop);
                    }
                }

                if (type.IsAssignableTo(typeof(ISyncProperty)))
                    properties.Add(type.GetProperty(nameof(ISyncProperty.SyncPropertyID))!);
            }

            T copy = (T)Activator.CreateInstance(type, args)!;
            foreach (var prop in _classProperties[type])
                prop.SetValue(copy, prop.GetValue(obj));

            return copy;
        }
    }
}
