using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPostReloadContext : WeaponTriggerContext
    {
        public WeaponPostReloadContext(BulletWeapon weapon) : base(weapon, TriggerType.OnReload) {}
    }
}
