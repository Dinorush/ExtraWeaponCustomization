using EWC.Attributes;
using EWC.CustomWeapon.ComponentWrapper.WeaponComps;
using EWC.CustomWeapon.WeaponContext.Contexts;
using Il2CppInterop.Runtime.Injection;
using ModifierAPI;
using System;

namespace EWC.CustomWeapon
{
    public sealed class CustomMeleeComponent : CustomWeaponComponent
    {
        public float CurrentAttackSpeed => _speedModifier.Mod;

        private readonly IStatModifier _speedModifier;
        private readonly IStatModifier _chargeSpeedModifier;

        public CustomMeleeComponent(IntPtr value) : base(value)
        {
            if (Owner.IsType(Enums.OwnerType.Local))
            {
                _speedModifier = MeleeAttackSpeedAPI.AddModifier(1f, group: "EWC");
                _chargeSpeedModifier = MeleeAttackSpeedAPI.AddChargeSpeedModifier(1f, group: "EWC");
            }
            else
            {
                _speedModifier = null!;
                _chargeSpeedModifier = null!;
            }
        }

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomMeleeComponent>();
        }

        public override void Clear()
        {
            base.Clear();
            if (Owner.IsType(Enums.OwnerType.Local))
            {
                _speedModifier.Disable();
                _speedModifier.Mod = 1f;
                _chargeSpeedModifier.Disable();
                _chargeSpeedModifier.Mod = 1f;
            }
        }

        public void UpdateAttackSpeed()
        {
            if (Owner.IsType(Enums.OwnerType.Local))
            {
                _speedModifier.Enable(Invoke(new WeaponFireRateContext(1f)).Value);
                _chargeSpeedModifier.Enable(Invoke(new WeaponChargeSpeedContext()).Value);
            }
        }
    }
}
