namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreReloadContext : IWeaponContext
    {
        public bool Allow { get; set; } = true;

        public WeaponPreReloadContext() { }
    }
}
