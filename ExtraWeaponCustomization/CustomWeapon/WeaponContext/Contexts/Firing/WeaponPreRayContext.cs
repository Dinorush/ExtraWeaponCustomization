using EWC.CustomWeapon.WeaponContext.Attributes;
using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    [RequireType(requiredOwnerType: Enums.OwnerType.Managed, requiredWeaponType: Enums.WeaponType.Gun)]
    public sealed class WeaponPreRayContext : IWeaponContext
    {
        public HitData Data { get; }
        public Vector3 Position { get; }

        public WeaponPreRayContext(HitData hitData, Vector3 position)
        {
            Data = hitData;
            Position = position;
        }
    }
}
