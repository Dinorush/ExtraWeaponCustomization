using UnityEngine;
using static Weapon;

namespace ExtraWeaponCustomization.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponPostRayContext : IWeaponContext
    {
        public WeaponHitData Data { get; }
        public Vector3 Position { get; }
        public bool Result { get; set; }

        public WeaponPostRayContext(WeaponHitData weaponHitData, Vector3 position, bool result)
        {
            Data = weaponHitData;
            Position = position;
            Result = result;
        }
    }
}
