namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponFireRateSetContext : IWeaponContext
    {
        public float FireRate { get; set; }

        public WeaponFireRateSetContext(float fireRate)
        {
            FireRate = fireRate;
        }
    }
}
