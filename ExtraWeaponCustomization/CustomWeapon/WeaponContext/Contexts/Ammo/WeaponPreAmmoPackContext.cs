namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreAmmoPackContext : IWeaponContext
    {
        public float AmmoAmount { get; set; }

        public WeaponPreAmmoPackContext(float ammoAmount)
        {
            AmmoAmount = ammoAmount;
        }
    }
}
