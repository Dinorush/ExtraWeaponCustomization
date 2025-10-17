using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(Enums.OwnerType.Managed, validWeaponType: Enums.WeaponType.Gun | Enums.WeaponType.SentryHolder)]
    public sealed class WeaponPreAmmoPackContext : IWeaponContext
    {
        public float AmmoAmount { get; set; }

        public WeaponPreAmmoPackContext(float ammoAmount)
        {
            AmmoAmount = ammoAmount;
        }
    }
}
