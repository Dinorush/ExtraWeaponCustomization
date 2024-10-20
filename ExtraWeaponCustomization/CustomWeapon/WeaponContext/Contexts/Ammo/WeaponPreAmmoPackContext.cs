namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPreAmmoPackContext : IWeaponContext
    {
        public float AmmoAmount { get; set; }

        public WeaponPreAmmoPackContext(float ammoAmount)
        {
            AmmoAmount = ammoAmount;
        }
    }
}
