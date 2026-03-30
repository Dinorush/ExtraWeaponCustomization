using EWC.CustomWeapon.WeaponContext.Attributes;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Local)]
    public sealed class WeaponPreSwapContext : IWeaponContext
    {
        public bool AllowBurstCancel { get; set; }
        private bool _allow = true;
        public bool Allow
        {
            get => _allow && AllowBurstCancel;
            set => _allow = value;
        }

        public WeaponPreSwapContext(bool allowNormal)
        {
            AllowBurstCancel = allowNormal;
        }
    }
}
