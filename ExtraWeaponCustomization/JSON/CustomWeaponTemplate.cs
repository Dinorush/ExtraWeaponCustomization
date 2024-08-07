﻿using ExtraWeaponCustomization.CustomWeapon;
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
                    new Explosive(),
                    new FireRateMod(),
                    new HealthMod(),
                    new RecoilMod(),

                    new Accelerate(),
                    new AmmoCap(),
                    new ArmorPierce(),
                    new AutoAim(),
                    new AutoTrigger(),
                    new BackstabMulti(),
                    new EnforceFireRate(),
                    new HoldBurst(),
                    new PierceMulti(),
                    new CustomWeapon.Properties.Traits.Projectile(),
                    new ReserveClip(),
                    new ThickBullet(),
                    new TumorMulti()
                }
            };
            return data;
        }
    }
}
