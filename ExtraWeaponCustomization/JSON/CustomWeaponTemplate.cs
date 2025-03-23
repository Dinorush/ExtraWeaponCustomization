using EWC.CustomWeapon;
using EWC.CustomWeapon.Properties;
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
                    new ReferenceProperty(),

                    new AmmoMod(),
                    new AmmoRegen(),
                    new ShotMod(),
                    new ShotModPerTarget(),
                    new DamageOverTime(),
                    new Explosive(),
                    new FireRateMod(),
                    new FireShot(),
                    new Foam(),
                    new HealthMod(),
                    new Noise(),
                    new RecoilMod(),
                    new ReferenceTrigger(),
                    new SpreadMod(),
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
                    new HitmarkerCooldown(),
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
