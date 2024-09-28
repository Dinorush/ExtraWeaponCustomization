namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponRecoilContext : WeaponStackModContext
    {
        public WeaponRecoilContext() : base(1f)
        {
            _stackMod.SetMin(float.MinValue);
        }
    }
}
