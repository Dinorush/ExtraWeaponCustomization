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
        public readonly MeleeComp Melee;

        public float CurrentAttackSpeed => _speedModifier.Mod;

        private readonly IStatModifier _speedModifier;
        private readonly IStatModifier _chargeSpeedModifier;

        public CustomMeleeComponent(IntPtr value) : base(value)
        {
            Melee = (MeleeComp)Weapon;
            _speedModifier = MeleeAttackSpeedAPI.AddModifier(1f, group: "EWC");
            _chargeSpeedModifier = MeleeAttackSpeedAPI.AddChargeSpeedModifier(1f, group: "EWC");
        }

        [InvokeOnLoad]
        private static void Init()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CustomMeleeComponent>();
        }

        public override void Clear()
        {
            base.Clear();
            _speedModifier.Disable();
            _speedModifier.Mod = 1f;
            _chargeSpeedModifier.Disable();
            _chargeSpeedModifier.Mod = 1f;
        }

        public void UpdateAttackSpeed()
        {
            _speedModifier.Enable(Invoke(new WeaponFireRateContext(1f)).Value);
            _chargeSpeedModifier.Enable(Invoke(new WeaponChargeSpeedContext()).Value);
        }
    }
}
