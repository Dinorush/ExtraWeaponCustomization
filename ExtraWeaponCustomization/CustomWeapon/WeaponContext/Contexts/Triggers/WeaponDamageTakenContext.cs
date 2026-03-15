using EWC.CustomWeapon.Enums;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponDamageTakenContext : WeaponTriggerContext
    {
        public float Damage { get; }
        public PlayerDamageType DamageType { get; }

        public WeaponDamageTakenContext(float damage, PlayerDamageType damageType) : base()
        {
            Damage = damage;
            DamageType = damageType;
        }
    }
}
