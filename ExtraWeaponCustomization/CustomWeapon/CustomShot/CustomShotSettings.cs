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
        public Func<Gear.BulletWeapon, HitData, bool> hitFunc;
        public int pierceLimit;

        public CustomShotSettings(ThickBullet? thickBullet = null, WallPierce? wallPierce = null, Properties.Traits.Projectile? projectile = null, Func<Gear.BulletWeapon, HitData, bool>? hitFunc = null, int pierceLimit = 1)
        {
            this.thickBullet = thickBullet;
            this.projectile = projectile;
            this.wallPierce = wallPierce;
            this.hitFunc = hitFunc ?? ShotManager.BulletHit;
            this.pierceLimit = pierceLimit;
        }

        public readonly CustomShotSettings Clone(Func<Gear.BulletWeapon, HitData, bool>? hitFunc = null)
        {
            return new(
                (ThickBullet?)thickBullet?.Clone(),
                (WallPierce?)wallPierce?.Clone(),
                (Properties.Traits.Projectile?)projectile?.Clone(),
                hitFunc,
                pierceLimit
            );
        }
    }
}
