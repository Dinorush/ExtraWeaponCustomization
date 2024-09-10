namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPreAmmoPackContext : IWeaponContext
    {
        public float AmmoRel { get; set; }

        public WeaponPreAmmoPackContext(float ammoRel)
        {
            AmmoRel = ammoRel;
        }
    }
}
