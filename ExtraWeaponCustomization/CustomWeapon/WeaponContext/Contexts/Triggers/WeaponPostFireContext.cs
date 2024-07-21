using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostFireContext : WeaponTriggerContext
    {
        public WeaponPostFireContext(BulletWeapon weapon) : base(weapon) {}
    }
}
