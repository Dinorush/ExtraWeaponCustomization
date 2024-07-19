using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponDamageFlagContext : WeaponTriggerContext
    {
        public DamageFlag DamageFlag { get; protected set; }

        public WeaponDamageFlagContext(BulletWeapon weapon, DamageFlag flag) : base(weapon)
        {
            DamageFlag = flag;
        }
    }
}
