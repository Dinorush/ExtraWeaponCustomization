using EWC.Utils;
using UnityEngine;

namespace EWC.CustomWeapon.WeaponContext.Contexts
{
    public sealed class WeaponCancelRayContext : IWeaponContext
    {
        public HitData Data { get; }
        public Vector3 Position { get; }
        public bool Result { get; set; }

        public WeaponCancelRayContext(HitData hitData, Vector3 position)
        {
            Data = hitData;
            Position = position;
            Result = true;
        }
    }
}
