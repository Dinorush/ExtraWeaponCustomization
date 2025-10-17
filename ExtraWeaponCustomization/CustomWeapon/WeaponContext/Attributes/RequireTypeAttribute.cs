using EWC.CustomWeapon.Enums;
using EWC.Utils.Extensions;
using System;

namespace EWC.CustomWeapon.WeaponContext.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
    internal sealed class RequireTypeAttribute : Attribute
    {
        public OwnerType RequiredOwnerType { get; set; }
        public OwnerType ValidOwnerType { get; set; }
        public WeaponType RequiredWeaponType { get; set; }
        public WeaponType ValidWeaponType { get; set; }
        public RequireTypeAttribute(
            OwnerType requiredOwnerType = OwnerType.Any,
            OwnerType validOwnerType = OwnerType.Any,
            WeaponType requiredWeaponType = WeaponType.Any,
            WeaponType validWeaponType = WeaponType.Any
            )
        {
            RequiredOwnerType = requiredOwnerType;
            ValidOwnerType = validOwnerType;
            RequiredWeaponType = requiredWeaponType;
            ValidWeaponType = validWeaponType;
        }

        public bool IsValid(OwnerType ownerType, WeaponType weaponType) =>
            ownerType.HasFlag(RequiredOwnerType) &&
            ownerType.HasAnyFlag(ValidOwnerType) &&
            weaponType.HasFlag(RequiredWeaponType) &&
            weaponType.HasAnyFlag(ValidWeaponType);
    }
}
