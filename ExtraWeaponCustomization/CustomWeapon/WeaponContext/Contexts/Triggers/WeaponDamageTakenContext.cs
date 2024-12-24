namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponDamageTakenContext : WeaponTriggerContext
    {
        public float Damage { get; }

        public WeaponDamageTakenContext(float damage) : base()
        {
            Damage = damage;
        }
    }
}
