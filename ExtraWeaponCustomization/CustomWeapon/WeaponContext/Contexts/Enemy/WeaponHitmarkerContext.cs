namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponHitmarkerContext : IWeaponContext
    {
        public bool Result { get; set; } = true;

        public WeaponHitmarkerContext() { }
    }
}
