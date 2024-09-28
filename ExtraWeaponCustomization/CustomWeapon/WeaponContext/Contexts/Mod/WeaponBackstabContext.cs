namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    internal class WeaponBackstabContext : WeaponStackModContext
    {
        public WeaponBackstabContext() : base(2)
        {
            _stackMod.SetMin(1f);
        }
    }
}
