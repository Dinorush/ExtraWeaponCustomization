using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Unmanaged, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponPostFireContextSync : IWeaponContext
    {
        public WeaponPostFireContextSync()
        {
        }
    }
}
