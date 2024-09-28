namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponArmorContext : IWeaponContext
    {
        public float ArmorMulti { get; set; }

        public WeaponArmorContext(float armor)
        {
            ArmorMulti = armor;
        }
    }
}
