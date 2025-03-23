using EWC.CustomWeapon.WeaponContext;
using System.Diagnostics.CodeAnalysis;

namespace EWC.CustomWeapon.Properties
{
    public static class PropertyExtensions
    {
        public static bool IsProperty<TContext>(this IWeaponProperty property) where TContext : IWeaponContext
        {
            return property.ShouldRegister(typeof(TContext)) && property is IWeaponProperty<TContext>;
        }

        public static bool IsProperty<TContext>(this IWeaponProperty property, [MaybeNullWhen(false)] out IWeaponProperty<TContext> castProperty) where TContext : IWeaponContext
        {
            if (property.ShouldRegister(typeof(TContext)) && property is IWeaponProperty<TContext> contextProperty)
            {
                castProperty = contextProperty;
                return true;
            }
            castProperty = default;
            return false;
        }
    }
}
