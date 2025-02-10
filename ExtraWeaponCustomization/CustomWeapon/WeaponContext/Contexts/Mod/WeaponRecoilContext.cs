namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponRecoilContext : WeaponStackModContext
    {
        public WeaponRecoilContext() : base(1f)
        {
            _stackMod.SetMin(float.MinValue);
        }
    }
}
