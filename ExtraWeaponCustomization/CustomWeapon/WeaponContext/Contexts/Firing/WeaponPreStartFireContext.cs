namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public class WeaponPreStartFireContext : IWeaponContext
    {
        public bool Allow { get; set; }

        public WeaponPreStartFireContext()
        {
            Allow = true;
        }
    }
}
