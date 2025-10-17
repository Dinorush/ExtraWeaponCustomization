using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Local, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponRecoilContext : WeaponStackModContext
    {
        public WeaponRecoilContext() : base(1f)
        {
            _stackMod.SetMin(float.MinValue);
        }
    }
}
