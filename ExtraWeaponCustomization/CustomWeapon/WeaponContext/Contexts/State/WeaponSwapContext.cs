using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Local, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponSwapContext : IWeaponContext
    {
        private bool _allow = true;
        public bool AllowInBurst { get; set; }
        public bool Allow
        {
            get => _allow && AllowInBurst;
            set => _allow = value;
        }

        public WeaponSwapContext(bool allowInBurst)
        {
            AllowInBurst = allowInBurst;
        }
    }
}
