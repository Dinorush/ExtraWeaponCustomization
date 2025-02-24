namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponCancelTracerContext : IWeaponContext
    {
        public bool Allow { get; set; } = true;

        public WeaponCancelTracerContext() { }
    }
}
