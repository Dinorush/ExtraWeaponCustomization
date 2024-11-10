using EWC.CustomWeapon;
using EWC.CustomWeapon.Properties.Effects;
using EWC.CustomWeapon.Properties.Traits;

namespace EWC.JSON
{
    internal static class CustomWeaponTemplate
    {
        internal static CustomWeaponData CreateTemplate()
        {
            CustomWeaponData data = new()
            {
                ArchetypeID = 0,
                Name = "Example",
                Properties = new(new()
                {
                    new AmmoMod(),
                    new AmmoRegen(),
                    new DamageMod(),
                    new DamageModPerTarget(),
                    new DamageOverTime(),
                    new Explosive(),
                    new FireRateMod(),
                    new HealthMod(),
                    new RecoilMod(),
                    new TempProperties(),

                    new Accelerate(),
                    new AmmoCap(),
                    new ArmorPierce(),
                    new AutoAim(),
                    new AutoTrigger(),
                    new BackstabMulti(),
                    new BioPing(),
                    new DataSwap(),
                    new EnforceFireRate(),
                    new HoldBurst(),
                    new MultiShot(),
                    new PierceMulti(),
                    new CustomWeapon.Properties.Traits.Projectile(),
                    new ReserveClip(),
                    new Silence(),
                    new ThickBullet(),
                    new TumorMulti(),
                    new WallPierce()
                })
            };
            return data;
        }
    }
}
