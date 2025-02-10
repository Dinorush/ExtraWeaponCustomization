namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreStartFireContext : IWeaponContext
    {
        public bool Allow { get; set; }

        public WeaponPreStartFireContext()
        {
            Allow = true;
        }
    }
}
