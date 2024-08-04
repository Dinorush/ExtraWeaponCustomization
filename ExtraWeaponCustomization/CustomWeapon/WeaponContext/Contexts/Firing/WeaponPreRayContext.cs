using Gear;
using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPreRayContext : IWeaponContext
    {
        public WeaponHitData Data { get; }
        public BulletWeapon Weapon { get; }
        public Vector3 Position { get; }

        public WeaponPreRayContext(WeaponHitData weaponHitData, Vector3 position, BulletWeapon weapon)
        {
            Data = weaponHitData;
            Weapon = weapon;
            Position = position;
        }
    }
}
