namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPierceContext : WeaponStackModContext
    {
        public IDamageable Damageable { get; }

        public WeaponPierceContext(float damage, IDamageable damageable) : base(damage)
        {
            Damageable = damageable;
        }
    }
}
