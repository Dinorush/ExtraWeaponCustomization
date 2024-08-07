using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    internal class WeaponBackstabContext : WeaponStackModContext
    {
        public WeaponBackstabContext(BulletWeapon weapon) : base(2, weapon)
        {
            _stackMod.SetMin(1f);
        }
    }
}
