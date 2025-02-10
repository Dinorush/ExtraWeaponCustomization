namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponBackstabContext : WeaponStackModContext
    {
        public WeaponBackstabContext() : base(2)
        {
            _stackMod.SetMin(1f);
        }
    }
}
