using Gear;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public abstract class WeaponDamageTypeContext : WeaponTriggerContext
    {
        public DamageType DamageType { get; protected set; }

        public WeaponDamageTypeContext(BulletWeapon weapon, DamageType flag) : base(weapon)
        {
            DamageType = flag;
        }
    }
}
