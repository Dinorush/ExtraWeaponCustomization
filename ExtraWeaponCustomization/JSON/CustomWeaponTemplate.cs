using EWC.CustomWeapon;
using EWC.CustomWeapon.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EWC.JSON
{
    internal static class CustomWeaponTemplate
    {
        internal static CustomWeaponData CreateTemplate()
        {
            var propTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.Namespace?.StartsWith(typeof(WeaponPropertyBase).Namespace!) == true);
            
            var effectTypes = propTypes
                .Where(t => t.Namespace!.EndsWith("Effects") && t.IsAssignableTo(typeof(WeaponPropertyBase)) && !t.IsAbstract)
                .OrderBy(t => t.Name);
            var traitTypes = propTypes
                .Where(t => t.Namespace!.EndsWith("Traits") && t.IsAssignableTo(typeof(WeaponPropertyBase)) && !t.IsAbstract)
                .OrderBy(t => t.Name);

            List<WeaponPropertyBase> examples = new() { new ReferenceProperty() };

            foreach (var type in effectTypes)
                examples.Add((WeaponPropertyBase)Activator.CreateInstance(type)!);

            foreach (var type in traitTypes)
                examples.Add((WeaponPropertyBase)Activator.CreateInstance(type)!);

            CustomWeaponData data = new()
            {
                ArchetypeID = 0,
                Name = "Example",
                Properties = new(examples)
            };
            return data;
        }
    }
}
