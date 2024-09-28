namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponDamageContext : IWeaponContext
    {
        public IDamageable Damageable { get; }
        public StackMod Damage { get; }
        public StackMod Precision { get; }

        public WeaponDamageContext(float damage, float precision, IDamageable damageable)
        {
            Damage = new(damage);
            Precision = new(precision);
            Damageable = damageable;
        }
    }
}
