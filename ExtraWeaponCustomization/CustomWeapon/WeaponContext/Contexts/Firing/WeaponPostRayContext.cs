using Gear;
using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostRayContext : IWeaponContext
    {
        public WeaponHitData Data { get; }
        public BulletWeapon Weapon { get; }
        public Vector3 Position { get; }

        public WeaponPostRayContext(WeaponHitData weaponHitData, Vector3 position, BulletWeapon weapon)
        {
            Data = weaponHitData;
            Weapon = weapon;
            Position = position;
        }
    }
}
