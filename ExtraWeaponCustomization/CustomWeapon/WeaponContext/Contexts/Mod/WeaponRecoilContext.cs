using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponRecoilContext : WeaponStackModContext
    {
        public WeaponRecoilContext(BulletWeapon weapon) : base(1f, weapon)
        {
            _stackMod.SetMin(float.MinValue);
        }
    }
}
