using EWC.CustomWeapon.ComponentWrapper;
using EWC.CustomWeapon.Properties.Traits;
using EWC.Utils;
using System;

namespace EWC.CustomWeapon.CustomShot
{
    public struct CustomShotSettings
    {
        public ThickBullet? thickBullet;
        public Properties.Traits.Projectile? projectile;
        public WallPierce? wallPierce;
        public Func<IWeaponComp, HitData, bool> hitFunc;

        public CustomShotSettings(ThickBullet? thickBullet = null, WallPierce? wallPierce = null, Properties.Traits.Projectile? projectile = null, Func<IWeaponComp, HitData, bool>? hitFunc = null)
        {
            this.thickBullet = thickBullet;
            this.projectile = projectile;
            this.wallPierce = wallPierce;
            this.hitFunc = hitFunc ?? ShotManager.BulletHit;
        }

        public readonly CustomShotSettings Clone(Func<IWeaponComp, HitData, bool>? hitFunc = null)
        {
            return new(
                (ThickBullet?)thickBullet?.Clone(),
                (WallPierce?)wallPierce?.Clone(),
                (Properties.Traits.Projectile?)projectile?.Clone(),
                hitFunc
            );
        }
    }
}
