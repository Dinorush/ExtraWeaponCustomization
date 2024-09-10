namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponFireCancelContext : IWeaponContext
    {
        public bool Allow { get; set; }

        public WeaponFireCancelContext()
        {
            Allow = true;
        }
    }
}
