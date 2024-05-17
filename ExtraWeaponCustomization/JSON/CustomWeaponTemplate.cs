using ExtraWeaponCustomization.CustomWeapon;
using ExtraWeaponCustomization.CustomWeapon.Properties.Effects;
using ExtraWeaponCustomization.CustomWeapon.Properties.Traits;

namespace ExtraWeaponCustomization.JSON
{
    internal static class CustomWeaponTemplate
    {
        internal static CustomWeaponData CreateTemplate()
        {
            CustomWeaponData data = new()
            {
                ArchetypeID = 0,
                Name = "Example",
                Properties = new()
                {
                    new AmmoMod(),
                    new DamageMod(),
                    new DamageModPerTarget(),
                    new DamageOverTime(),
                    new FireRateMod(),

                    new AmmopackRefill(),
                    new AutoAim(),
                    new AutoBurst(),
                    new EnforceFireRate(),
                    new Explosive(),
                    new FireRateAccel(),
                    new HoldBurst(),
                    new ReserveClip()
                }
            };
            return data;
        }
    }
}
